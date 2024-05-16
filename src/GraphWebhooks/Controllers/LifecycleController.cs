// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using GraphWebhooks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace GraphWebhooks.Controllers;

/// <summary>
/// Implements the lifecycle notification endpoint which receives
/// notifications from Microsoft Graph.
/// </summary>
public class LifecycleController(
    GraphServiceClient graphClient,
    SubscriptionStore subscriptionStore,
    ILogger<LifecycleController> logger) : Controller
{
    private readonly GraphServiceClient graphClient = graphClient ??
        throw new ArgumentException(nameof(graphClient));

    private readonly SubscriptionStore subscriptionStore = subscriptionStore ??
        throw new ArgumentException(nameof(subscriptionStore));

    private readonly ILogger<LifecycleController> logger = logger ??
        throw new ArgumentException(nameof(logger));

    /// <summary>
    /// POST /lifecycle.
    /// </summary>
    /// <param name="validationToken">Optional. Validation token sent by Microsoft Graph during endpoint validation phase.</param>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] string? validationToken = null)
    {
        // If there is a validation token in the query string,
        // send it back in a 200 OK text/plain response
        if (!string.IsNullOrEmpty(validationToken))
        {
            return Ok(validationToken);
        }

        // Use the Graph client's serializer to deserialize the body
        using var bodyStream = new MemoryStream();
        await Request.Body.CopyToAsync(bodyStream);
        bodyStream.Seek(0, SeekOrigin.Begin);

        // Calling RegisterDefaultDeserializer here isn't strictly necessary since
        // we have a GraphServiceClient instance. In cases where you do not have a
        // GraphServiceClient, you need to register the JSON provider before trying
        // to deserialize.
        ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
        var notifications = KiotaJsonSerializer.Deserialize<ChangeNotificationCollection>(bodyStream);

        if (notifications == null || notifications.Value == null)
        {
            return Accepted();
        }

        // Process any lifecycle events
        var lifecycleNotifications = notifications.Value.Where(n => n.LifecycleEvent != null);
        foreach (var lifecycleNotification in lifecycleNotifications)
        {
            logger.LogInformation(
                "Received {eventType} notification for subscription {subscriptionId}",
                lifecycleNotification.LifecycleEvent.ToString(),
                lifecycleNotification.SubscriptionId);

            if (lifecycleNotification.LifecycleEvent == LifecycleEventType.ReauthorizationRequired)
            {
                // The subscription needs to be renewed
                try
                {
                    await RenewSubscriptionAsync(lifecycleNotification);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error renewing subscription");
                }
            }
        }

        // Return 202 to Graph to confirm receipt of notification.
        // Not sending this will cause Graph to retry the notification.
        return Accepted();
    }

    private async Task RenewSubscriptionAsync(ChangeNotification lifecycleNotification)
    {
        var subscriptionId = lifecycleNotification.SubscriptionId?.ToString();

        if (!string.IsNullOrEmpty(subscriptionId))
        {
            var subscription = subscriptionStore.GetSubscriptionRecord(subscriptionId);
            if (subscription != null &&
                !string.IsNullOrEmpty(subscription.UserId) &&
                !string.IsNullOrEmpty(subscription.TenantId))
            {
                var isAppOnly = subscription.UserId.Equals("APP-ONLY", StringComparison.OrdinalIgnoreCase);
                if (!isAppOnly)
                {
                    // Since the POST comes from Graph, there's no user in the context
                    // Set the user to the user that owns the message. This will enable
                    // Microsoft.Identity.Web to acquire the proper token for the proper user
                    HttpContext.User = ClaimsPrincipalFactory
                        .FromTenantIdAndObjectId(subscription.TenantId, subscription.UserId);
                    HttpContext.User.AddMsalInfo(subscription.UserId, subscription.TenantId);
                }

                var update = new Subscription
                {
                    ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1),
                };

                await graphClient.Subscriptions[subscriptionId]
                    .PatchAsync(update, req =>
                    {
                        req.Options.WithAppOnly(isAppOnly);
                    });

                logger.LogInformation("Renewed subscription");
            }
        }
    }
}
