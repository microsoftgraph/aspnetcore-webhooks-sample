// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using GraphWebhooks.Models;
using GraphWebhooks.Services;

namespace GraphWebhooks.Controllers
{
    public class WatchController : Controller
    {
        private readonly GraphServiceClient _graphClient;
        private readonly SubscriptionStore _subscriptionStore;
        private readonly ILogger<WatchController> _logger;
        private readonly string _notificationHost;

        public WatchController(
          GraphServiceClient graphClient,
            SubscriptionStore subscriptionStore,
            ILogger<WatchController> logger,
            IConfiguration config)
        {
            _graphClient = graphClient;
            _subscriptionStore = subscriptionStore;
            _logger = logger;

            _notificationHost = config.GetValue<string>("NotificationHost");
            if (string.IsNullOrEmpty(_notificationHost) || _notificationHost == "YOUR_NGROK_PROXY")
            {
                throw new ArgumentException("You must configure NotificationHost in appsettings.json");
            }
        }

        [AuthorizeForScopes(ScopeKeySection = "GraphScopes")]
        public async Task<IActionResult> Delegated()
        {
            try
            {
                string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                _logger.LogInformation($"Authenticated user ID {userId}");

                var user = await _graphClient.Me
                    .Request()
                    .Select(u => new {u.DisplayName, u.Mail})
                    .GetAsync();

                _logger.LogInformation($"Authenticated user: {user.DisplayName} ({user.Mail})");

                var subscription = new Subscription
                {
                    ChangeType = "created",
                    NotificationUrl = $"{_notificationHost}/listen",
                    Resource = "me/mailfolders/inbox/messages",
                    ClientState = Guid.NewGuid().ToString(),
                    IncludeResourceData = false,
                    ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1)
                };

                var newSubscription = await _graphClient.Subscriptions
                    .Request().AddAsync(subscription);

                _subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
                {
                    Id = newSubscription.Id,
                    UserId = userId,
                    ClientState = newSubscription.ClientState
                });

                return View(newSubscription).WithSuccess("Subscription created");
            }
            catch (Exception ex)
            {
                if (ex.InnerException is MicrosoftIdentityWebChallengeUserException) throw;
                return View().WithError($"Error creating subscription: {ex.Message}",
                    ex.ToString());
            }
        }

        public async Task<IActionResult> Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                return RedirectToAction("Index", "Home")
                    .WithError("No subscription ID specified");
            }

            try
            {
                await _graphClient.Subscriptions[subscriptionId]
                    .Request()
                    .DeleteAsync();

                _subscriptionStore.DeleteSubscriptionRecord(subscriptionId);

                return RedirectToAction("SignOut", "Account", new { area = "MicrosoftIdentity" });
            }
            catch (Exception ex)
            {
                if (ex.InnerException is MicrosoftIdentityWebChallengeUserException) throw;
                return RedirectToAction("Index", "Home")
                    .WithError($"Error deleting subscription: {ex.Message}",
                        ex.ToString());
            }
        }
    }
}
