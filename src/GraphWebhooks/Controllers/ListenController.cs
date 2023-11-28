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
using Microsoft.Kiota.Serialization.Json;

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

        var appId = configuration.GetValue<string>("AzureAd:ClientId") ??
            throw new Exception("AzureAd:ClientId missing in app settings");
        var tenantId = configuration.GetValue<string>("AzureAd:TenantId") ??
            throw new Exception("AzureAd:TenantId missing in app settings");
        _appIds = new List<Guid> { new Guid(appId) };
        _tenantIds = new List<Guid> { new Guid(tenantId) };
    }

    /// <summary>
    /// POST /listen
    /// </summary>
    /// <param name="validationToken">Optional. Validation token sent by Microsoft Graph during endpoint validation phase</param>
    /// <returns>IActionResult</returns>
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

        // Read the body
        using var reader = new StreamReader(Request.Body);

        // Use the Graph client's serializer to deserialize the body
        var bodyStream = new MemoryStream();
        await Request.Body.CopyToAsync(bodyStream);
        bodyStream.Seek(0, SeekOrigin.Begin);
        var parseNode = new JsonParseNodeFactory().GetRootParseNode("application/json", bodyStream);
        var notifications = parseNode.GetObjectValue(ChangeNotificationCollection.CreateFromDiscriminatorValue);

        if (notifications == null || notifications.Value == null) return Ok();

        // Validate any tokens in the payload
        var areTokensValid = await notifications.AreTokensValid(_tenantIds, _appIds);
        if (!areTokensValid) return Unauthorized();

        // Process non-encrypted notifications first
        // These will be notifications for user mailbox
        var messageNotifications = new Dictionary<string, ChangeNotification>();
        foreach (var notification in notifications.Value.Where(n => n.EncryptedContent == null))
        {
            // Find the subscription in our store
            var subscription = _subscriptionStore
                .GetSubscriptionRecord(notification.SubscriptionId.ToString() ?? string.Empty);

            // If this isn't a subscription we know about, or if client state doesn't match,
            // ignore it
            if (subscription != null && subscription.ClientState == notification.ClientState)
            {
                _logger.LogInformation($"Received notification for: {notification.Resource}");
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
                    var cert = await _certificateService.GetDecryptionCertificate();
                    return cert;
                });

                // Add a SignalR notification for this message to the list
                if (chatMessage != null)
                {
                    clientNotifications.Add(new ClientNotification(new
                    {
                        Sender = chatMessage.From?.User?.DisplayName ?? "UNKNOWN",
                        Message = chatMessage.Body?.Content ?? ""
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
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
    /// Gets each message specified in a set of notifications
    /// </summary>
    /// <param name="notifications">A set of notifications for new messages</param>
    private async Task GetMessagesAsync(IEnumerable<ChangeNotification> notifications)
    {
        var clientNotifications = new List<ClientNotification>();

        foreach(var notification in notifications)
        {
            // Get the subscription from the store for user/tenant ID
            var subscription = _subscriptionStore.GetSubscriptionRecord(notification.SubscriptionId.ToString() ?? string.Empty);

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
            var request = new Microsoft.Kiota.Abstractions.RequestInformation
            {
                HttpMethod = Microsoft.Kiota.Abstractions.Method.GET,
                URI = new Uri($"{_graphClient.RequestAdapter.BaseUrl}/{notification.Resource}"),
            };

            var message = await _graphClient.RequestAdapter.SendAsync(request, Message.CreateFromDiscriminatorValue);

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
            await _hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
        }
    }
}
