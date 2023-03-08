// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraphWebhooks.Models;
using GraphWebhooks.Services;
using GraphWebhooks.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace GraphWebhooks.Controllers;

/// <summary>
/// Implements the notification endpoint which receives
/// notifications from Microsoft Graph
/// </summary>
public class ListenController : Controller
{
    private readonly GraphServiceClient _graphClient;
    private readonly SubscriptionStore _subscriptionStore;
    private readonly CertificateService _certificateService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ListenController> _logger;
    private readonly List<Guid> _appIds;
    private readonly List<Guid> _tenantIds;

    public ListenController(
        GraphServiceClient graphClient,
        SubscriptionStore subscriptionStore,
        CertificateService certificateService,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration,
        ILogger<ListenController> logger)
    {
        _graphClient = graphClient ?? throw new ArgumentException(nameof(graphClient));
        _subscriptionStore = subscriptionStore ?? throw new ArgumentException(nameof(subscriptionStore));
        _certificateService = certificateService ?? throw new ArgumentException(nameof(certificateService));
        _hubContext = hubContext ?? throw new ArgumentException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
        _ = configuration ?? throw new ArgumentException(nameof(configuration));

        _appIds = new List<Guid> { new Guid(configuration.GetValue<string>("AzureAd:ClientId")) };
        _tenantIds = new List<Guid> { new Guid(configuration.GetValue<string>("AzureAd:TenantId")) };
    }

    /// <summary>
    /// POST /listen
    /// </summary>
    /// <param name="validationToken">Optional. Validation token sent by Microsoft Graph during endpoint validation phase</param>
    /// <returns>IActionResult</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] string validationToken = null)
    {
        // If there is a validation token in the query string,
        // send it back in a 200 OK text/plain response
        if (!string.IsNullOrEmpty(validationToken))
        {
            return Ok(validationToken);
        }

        // Read the body
        using var reader = new StreamReader(Request.Body);
        var jsonPayload = await reader.ReadToEndAsync();

        // Use the Graph client's serializer to deserialize the body
        var notifications = _graphClient.HttpProvider.Serializer
            .DeserializeObject<ChangeNotificationCollection>(jsonPayload);

        if (notifications == null) return Ok();

        // Validate any tokens in the payload
        var areTokensValid = await AreTokensValid(notifications, _tenantIds, _appIds);
        if (!areTokensValid) return Unauthorized();

        // Process non-encrypted notifications first
        // These will be notifications for user mailbox
        var messageNotifications = new Dictionary<string, ChangeNotification>();
        foreach (var notification in notifications.Value.Where(n => n.EncryptedContent == null))
        {
            // Find the subscription in our store
            var subscription = _subscriptionStore
                .GetSubscriptionRecord(notification.SubscriptionId.ToString());

            // If this isn't a subscription we know about, or if client state doesnt' match,
            // ignore it
            if (subscription != null && subscription.ClientState == notification.ClientState)
            {
                _logger.LogInformation($"Received notification for: {notification.Resource}");
                // Add notification to list to process. If there is more than
                // one notification for a given resource, we'll only process it once
                messageNotifications[notification.Resource] = notification;
            }
        }

        // Since resource data is not included in these notifications,
        // use Microsoft Graph to get the messages
        await GetMessagesAsync(messageNotifications.Values);

        // Process encrypted notifications
        var clientNotifications = new List<ClientNotification>();
        foreach (var notification in notifications.Value.Where(n => n.EncryptedContent != null))
        {
            // Decrypt the encrypted payload using private key
            var chatMessage = await notification.EncryptedContent.DecryptAsync<ChatMessage>((id, thumbprint) => {
                return _certificateService.GetDecryptionCertificate();
            });

            // Add a SignalR notification for this message to the list
            clientNotifications.Add(new ClientNotification(new
            {
                Sender = chatMessage.From?.User?.DisplayName ?? "UNKNOWN",
                Message = chatMessage.Body?.Content ?? ""
            }));
        }

        // Send SignalR notifications
        if (clientNotifications.Count > 0)
        {
            await _hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
        }

        // Return 202 to Graph to confirm receipt of notification.
        // Not sending this will cause Graph to retry the notification.
        return Accepted();
    }

    /// <summary>
    /// Validates tokens contained in a ChangeNotificationCollection
    /// </summary>
    /// <param name="notifications">The ChangeNotificationCollection to validate</param>
    /// <param name="tenantIds">A list of expected tenant IDs</param>
    /// <param name="appIds">A list of expected app IDs</param>
    /// <returns>true if all tokens are valid, false if not</returns>
    private async Task<bool> AreTokensValid(
        ChangeNotificationCollection notifications,
        List<Guid> tenantIds,
        List<Guid> appIds)
    {
        // First validate assuming tokens are v2 tokens
        bool areTokensValid = await notifications.AreTokensValidV2(tenantIds, appIds);

        if (!areTokensValid)
        {
            // If v2 validation failed, try v1
            // This method throws if validation fails, so catch any exception
            // and treat as validation failure
            try
            {
                areTokensValid = await notifications.AreTokensValid(tenantIds, appIds);
            }
            catch (Microsoft.IdentityModel.Tokens.SecurityTokenValidationException)
            {
                areTokensValid = false;
            }
        }

        return areTokensValid;
    }

    /// <summary>
    /// Gets each message specified in a set of notifications
    /// </summary>
    /// <param name="notifications">A set of notifications for new messages</param>
    private async Task GetMessagesAsync(IEnumerable<ChangeNotification> notifications)
    {
        var clientNotifications = new List<ClientNotification>();

        foreach(var notification in notifications)
        {
            // Get the subscription from the store for user/tenant ID
            var subscription = _subscriptionStore.GetSubscriptionRecord(notification.SubscriptionId.ToString());

            if (subscription != null && !string.IsNullOrEmpty(subscription.UserId))
            {
                // Since the POST comes from Graph, there's no user in the context
                // Set the user to the user that owns the message. This will enable
                // Microsoft.Identity.Web to acquire the proper token for the proper user
                HttpContext.User = ClaimsPrincipalFactory
                    .FromTenantIdAndObjectId(subscription.TenantId, subscription.UserId);
            }

            // The notification has the relative URL to the message in the Resource
            // property, so build the request using that information
            var request = new MessageRequest(
                $"{_graphClient.BaseUrl}/{notification.Resource}",
                _graphClient,
                null);

            var message = await request.GetAsync();

            // Add a SignalR notification for the message
            clientNotifications.Add(new ClientNotification(new
            {
                Subject = message.Subject ?? "",
                Id = message.Id
            }));
        }

        // Send SignalR notifications
        if (clientNotifications.Count > 0)
        {
            await _hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
        }
    }
}
