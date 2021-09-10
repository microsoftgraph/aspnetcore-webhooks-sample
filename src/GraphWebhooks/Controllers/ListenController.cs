// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraphWebhooks.Models;
using GraphWebhooks.Services;
using GraphWebhooks.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace GraphWebhooks.Controllers
{
    public class ListenController : Controller
    {
        private readonly GraphServiceClient _graphClient;
        private readonly SubscriptionStore _subscriptionStore;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ListenController> _logger;

        public ListenController(
            GraphServiceClient graphClient,
            SubscriptionStore subscriptionStore,
            IHubContext<NotificationHub> hubContext,
            ILogger<ListenController> logger)
        {
            _graphClient = graphClient;
            _subscriptionStore = subscriptionStore;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index([FromQuery] string validationToken = null)
        {
            if (!string.IsNullOrEmpty(validationToken))
            {
                return Ok(validationToken);
            }

            using var reader = new StreamReader(Request.Body);
            var jsonPayload = await reader.ReadToEndAsync();

            var notifications = _graphClient.HttpProvider.Serializer
                .DeserializeObject<ChangeNotificationCollection>(jsonPayload);

            if (notifications == null) return Ok();

            // Process non-encrypted notifications first
            // These will be notifications for user mailbox
            var messageNotifications = new Dictionary<string, ChangeNotification>();
            foreach (var notification in notifications.Value.Where(n => n.EncryptedContent == null))
            {
                var subscription = _subscriptionStore
                    .GetSubscriptionRecord(notification.SubscriptionId.ToString());

                if (subscription != null && subscription.ClientState == notification.ClientState)
                {
                    _logger.LogInformation($"Received notification for: {notification.Resource}");
                    messageNotifications[notification.Resource] = notification;
                }
            }

            await GetMessagesAsync(messageNotifications.Values);

            return Accepted();
        }

        private async Task GetMessagesAsync(IEnumerable<ChangeNotification> notifications)
        {
            var clientNotifications = new List<ClientNotification>();

            foreach(var notification in notifications)
            {
                var subscription = _subscriptionStore.GetSubscriptionRecord(notification.SubscriptionId.ToString());

                if (subscription != null && !string.IsNullOrEmpty(subscription.UserId))
                {
                    // Since the POST comes from Graph, there's no user in the context
                    // Set the user to the user that owns the message. This will enable
                    // Microsoft.Identity.Web to acquire the proper token for the proper user
                    HttpContext.User = ClaimsPrincipalFactory
                        .FromTenantIdAndObjectId(subscription.TenantId, subscription.UserId);
                }

                var request = new MessageRequest(
                    $"{_graphClient.BaseUrl}/{notification.Resource}",
                    _graphClient,
                    null);

                var message = await request.GetAsync();

                clientNotifications.Add(new ClientNotification(new
                {
                    Subject = message.Subject ?? "",
                    Id = message.Id
                }));
            }

            if (clientNotifications.Count > 0)
            {
                await _hubContext.Clients.All.SendAsync("showNotification", clientNotifications);
            }
        }
    }
}
