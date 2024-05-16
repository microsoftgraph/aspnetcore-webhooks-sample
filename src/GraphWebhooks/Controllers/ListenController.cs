// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using GraphWebhooks.Models;
using GraphWebhooks.Services;
using GraphWebhooks.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace GraphWebhooks.Controllers;

/// <summary>
/// Implements the notification endpoint which receives
/// notifications from Microsoft Graph.
/// </summary>
public class ListenController : Controller
{
    private readonly GraphServiceClient graphClient;
    private readonly SubscriptionStore subscriptionStore;
    private readonly CertificateService certificateService;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<ListenController> logger;
    private readonly List<Guid> appIds;
    private readonly List<Guid> tenantIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenController"/> class.
    /// </summary>
    /// <param name="graphClient">The <see cref="GraphServiceClient"/>.</param>
    /// <param name="subscriptionStore">The subscription store.</param>
    /// <param name="certificateService">The certificate service.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="configuration">The app configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentException">Thrown if any parameter is null.</exception>
    /// <exception cref="Exception">Thrown if configuration values are missing.</exception>
    public ListenController(
        GraphServiceClient graphClient,
        SubscriptionStore subscriptionStore,
        CertificateService certificateService,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration,
        ILogger<ListenController> logger)
    {
        this.graphClient = graphClient ??
            throw new ArgumentException("GraphServiceClient cannot be null", nameof(graphClient));
        this.subscriptionStore = subscriptionStore ??
            throw new ArgumentException("SubscriptionStore cannot be null", nameof(subscriptionStore));
        this.certificateService = certificateService ??
            throw new ArgumentException("CertificateService cannot be null", nameof(certificateService));
        this.hubContext = hubContext ??
            throw new ArgumentException("IHubContext cannot be null", nameof(hubContext));
        this.logger = logger ??
            throw new ArgumentException("ILogger cannot be null", nameof(logger));
        _ = configuration ??
            throw new ArgumentException("IConfiguration cannot be null", nameof(configuration));

        var appId = configuration.GetValue<string>("AzureAd:ClientId") is string appIdValue &&
            !string.IsNullOrEmpty(appIdValue) ? appIdValue :
            throw new Exception("AzureAd:ClientId missing in app settings");
        var tenantId = configuration.GetValue<string>("AzureAd:TenantId") is string tenantIdValue &&
            !string.IsNullOrEmpty(tenantIdValue) ? tenantIdValue :
            throw new Exception("AzureAd:TenantId missing in app settings");
        appIds = [new Guid(appId)];
        tenantIds = [new Guid(tenantId)];
    }

    /// <summary>
    /// POST /listen.
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

        // Validate any tokens in the payload
        var areTokensValid = await notifications.AreTokensValid(tenantIds, appIds);
        if (!areTokensValid)
        {
            return Unauthorized();
        }

        // Process non-encrypted notifications first
        // These will be notifications for user mailbox
        var messageNotifications = new Dictionary<string, ChangeNotification>();
        foreach (var notification in notifications.Value.Where(n => n.EncryptedContent == null))
        {
            // Find the subscription in our store
            var subscription = subscriptionStore
                .GetSubscriptionRecord(notification.SubscriptionId.ToString() ?? string.Empty);

            // If this isn't a subscription we know about, or if client state doesn't match,
            // ignore it
            if (subscription != null && subscription.ClientState == notification.ClientState)
            {
                logger.LogInformation("Received notification for: {resource}", notification.Resource);

                // Add notification to list to process. If there is more than
                // one notification for a given resource, we'll only process it once
                messageNotifications[notification.Resource!] = notification;
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
            try
            {
                var chatMessage = await notification.EncryptedContent!.DecryptAsync<ChatMessage>(async (id, thumbprint) =>
                {
                    var cert = await certificateService.GetDecryptionCertificate();
                    return cert;
                });

                // Add a SignalR notification for this message to the list
                if (chatMessage != null)
                {
                    clientNotifications.Add(new ClientNotification(new
                    {
                        Sender = chatMessage.From?.User?.DisplayName ?? "UNKNOWN",
                        Message = chatMessage.Body?.Content ?? string.Empty,
                    }));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{message}", ex.Message);
                throw;
            }
        }

        // Send SignalR notifications
        if (clientNotifications.Count > 0)
        {
            await hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
        }

        // Return 202 to Graph to confirm receipt of notification.
        // Not sending this will cause Graph to retry the notification.
        return Accepted();
    }

    /// <summary>
    /// Gets each message specified in a set of notifications.
    /// </summary>
    /// <param name="notifications">A set of notifications for new messages.</param>
    private async Task GetMessagesAsync(IEnumerable<ChangeNotification> notifications)
    {
        var clientNotifications = new List<ClientNotification>();

        foreach (var notification in notifications)
        {
            // Get the subscription from the store for user/tenant ID
            var subscription = subscriptionStore.GetSubscriptionRecord(notification.SubscriptionId.ToString() ?? string.Empty);

            if (subscription != null &&
                !string.IsNullOrEmpty(subscription.UserId) &&
                !string.IsNullOrEmpty(subscription.TenantId))
            {
                // Since the POST comes from Graph, there's no user in the context
                // Set the user to the user that owns the message. This will enable
                // Microsoft.Identity.Web to acquire the proper token for the proper user
                HttpContext.User = ClaimsPrincipalFactory
                    .FromTenantIdAndObjectId(subscription.TenantId, subscription.UserId);
                HttpContext.User.AddMsalInfo(subscription.UserId, subscription.TenantId);
            }

            // The notification has the relative URL to the message in the Resource
            // property, so build the request using that information
            var request = new RequestInformation
            {
                HttpMethod = Method.GET,
                URI = new Uri($"{graphClient.RequestAdapter.BaseUrl}/{notification.Resource}"),
            };

            var message = await graphClient.RequestAdapter.SendAsync(request, Message.CreateFromDiscriminatorValue);

            // Add a SignalR notification for the message
            clientNotifications.Add(new ClientNotification(new
            {
                Subject = message?.Subject ?? string.Empty,
                Id = message?.Id ?? string.Empty,
            }));
        }

        // Send SignalR notifications
        if (clientNotifications.Count > 0)
        {
            await hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
        }
    }
}
