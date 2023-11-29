// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using GraphWebhooks.Models;
using GraphWebhooks.Services;

namespace GraphWebhooks.Controllers;

/// <summary>
/// Implements subscription management endpoints
/// </summary>
public class WatchController : Controller
{
    private readonly GraphServiceClient _graphClient;
    private readonly SubscriptionStore _subscriptionStore;
    private readonly CertificateService _certificateService;
    private readonly ILogger<WatchController> _logger;
    private readonly string _notificationHost;

    public WatchController(
        GraphServiceClient graphClient,
        SubscriptionStore subscriptionStore,
        CertificateService certificateService,
        ILogger<WatchController> logger,
        IConfiguration configuration)
    {
        _graphClient = graphClient ?? throw new ArgumentException(nameof(graphClient));
        _subscriptionStore = subscriptionStore ?? throw new ArgumentException(nameof(subscriptionStore));
        _certificateService = certificateService ?? throw new ArgumentException(nameof(certificateService));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
        _ = configuration ?? throw new ArgumentException(nameof(configuration));

        _notificationHost = configuration.GetValue<string>("NotificationHost") is string hostValue &&
            !string.IsNullOrEmpty(hostValue) && !hostValue.Equals("YOUR_NGROK_PROXY", StringComparison.OrdinalIgnoreCase) ? hostValue :
            throw new ArgumentException("You must configure NotificationHost in appsettings.json");
    }

    /// <summary>
    /// GET /watch/delegated
    /// Creates a new subscription to the authenticated user's inbox and
    /// displays a page that updates with each received notification
    /// </summary>
    /// <returns></returns>
    [AuthorizeForScopes(ScopeKeySection = "GraphScopes")]
    public async Task<IActionResult> Delegated()
    {
        try
        {
            // Delete any existing subscriptions for the user
            await DeleteAllSubscriptions(false);

            // Get the user's ID and tenant ID from the user's identity
            var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            _logger.LogInformation($"Authenticated user ID {userId}");
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            // Get the user from Microsoft Graph
            var user = await _graphClient.Me.GetAsync(req =>
            {
                req.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
            });

            if (user == null)
            {
                _logger.LogWarning("Could not retrieve authenticated user.");
                return View().WithError("Could not retrieve authenticated user.");
            }

            _logger.LogInformation($"Authenticated user: {user.DisplayName} ({user.Mail ?? user.UserPrincipalName})");
            // Add the user's display name and email address to the user's
            // identity.
            User.AddUserGraphInfo(user);

            // Create the subscription
            var subscription = new Subscription
            {
                ChangeType = "created",
                NotificationUrl = $"{_notificationHost}/listen",
                LifecycleNotificationUrl = $"{_notificationHost}/lifecycle",
                Resource = "me/mailfolders/inbox/messages",
                ClientState = Guid.NewGuid().ToString(),
                IncludeResourceData = false,
                // Subscription only lasts for one hour
                ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1)
            };

            var newSubscription = await _graphClient.Subscriptions
                .PostAsync(subscription);

            if (newSubscription == null)
            {
                return View().WithError("No subscription was returned.");
            }

            // Add the subscription to the subscription store
            _subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
            {
                Id = newSubscription.Id,
                UserId = userId,
                TenantId = tenantId,
                ClientState = newSubscription.ClientState
            });

            return View(newSubscription).WithSuccess("Subscription created");
        }
        catch (Exception ex)
        {
            // Throw MicrosoftIdentityWebChallengeUserException to allow
            // Microsoft.Identity.Web to challenge the user for re-auth or consent
            if (ex.InnerException is MicrosoftIdentityWebChallengeUserException) throw;

            // Otherwise display the error
            return View().WithError($"Error creating subscription: {ex.Message}",
                ex.ToString());
        }
    }

    /// <summary>
    /// GET /watch/apponly
    /// Creates a new subscription to all Teams channel messages and
    /// displays a page that updates with each received notification
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> AppOnly()
    {
        try
        {
            // Delete any existing Teams channel subscriptions
            // This is important as each app is only allowed one active
            // subscription to the /teams/getAllMessages resource
            await DeleteAllSubscriptions(true);

            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            // Get the encryption certificate (public key)
            var encryptionCertificate = await _certificateService.GetEncryptionCertificate();

            // Create the subscription
            // This should work with just the base Subscription object, blocked by bug:
            // https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2237
            var subscription = new EncryptableSubscription
            {
                ChangeType = "created",
                NotificationUrl = $"{_notificationHost}/listen",
                LifecycleNotificationUrl = $"{_notificationHost}/lifecycle",
                Resource = "/teams/getAllMessages",
                ClientState = Guid.NewGuid().ToString(),
                IncludeResourceData = true,
                ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1),
                // To get resource data, we must provide a public key that
                // Microsoft Graph will use to encrypt their key
                // See https://docs.microsoft.com/graph/webhooks-with-resource-data#creating-a-subscription
                EncryptionCertificateId = encryptionCertificate.Subject,
            };

            subscription.AddPublicEncryptionCertificate(encryptionCertificate);


            var newSubscription = await _graphClient.Subscriptions.PostAsync(subscription, req =>
            {
                req.Options.WithAppOnly();
            });

            if (newSubscription == null)
            {
                return RedirectToAction("Index", "Home")
                    .WithError("No subscription was returned.");
            }

            // Add the subscription to the subscription store
            _subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
            {
                Id = newSubscription.Id,
                UserId = "APP-ONLY",
                TenantId = tenantId,
                ClientState = newSubscription.ClientState
            });

            return View(newSubscription).WithSuccess("Subscription created");
        }
        catch (Exception ex)
        {
            return RedirectToAction("Index", "Home")
                .WithError($"Error creating subscription: {ex.Message}",
                    ex.ToString());
        }
    }

    /// <summary>
    /// GET /watch/unsubscribe
    /// Deletes the user's inbox subscription and signs the user out
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete</param>
    /// <returns></returns>
    public async Task<IActionResult> Unsubscribe(string subscriptionId)
    {
        if (string.IsNullOrEmpty(subscriptionId))
        {
            return RedirectToAction("Index", "Home")
                .WithError("No subscription ID specified");
        }

        try
        {
            var subscription = _subscriptionStore.GetSubscriptionRecord(subscriptionId);

            if (subscription != null)
            {
                var appOnly = subscription.UserId == "APP-ONLY";
                // To unsubscribe, just delete the subscription
                await _graphClient.Subscriptions[subscriptionId]
                    .DeleteAsync(req =>
                    {
                        req.Options.WithAppOnly(appOnly);
                    });

                // Remove the subscription from the subscription store
                _subscriptionStore.DeleteSubscriptionRecord(subscriptionId);
            }
        }
        catch (Exception ex)
        {
            // Throw MicrosoftIdentityWebChallengeUserException to allow
            // Microsoft.Identity.Web to challenge the user for re-auth or consent
            if (ex.InnerException is MicrosoftIdentityWebChallengeUserException) throw;

            // Otherwise log the error
            _logger.LogError(ex, "Error deleting subscription");
        }

        // Redirect to Microsoft.Identity.Web's signout page
        return RedirectToAction("SignOut", "Account", new { area = "MicrosoftIdentity" });
    }

    /// <summary>
    /// Deletes all current subscriptions
    /// </summary>
    /// <param name="appOnly">If true, all app-only subscriptions are removed. If false, all user subscriptions are removed</param>
    private async Task DeleteAllSubscriptions(bool appOnly)
    {
        try
        {
            // Get all current subscriptions
            var subscriptions = await _graphClient.Subscriptions
                .GetAsync(req =>
                {
                    req.Options.WithAppOnly(appOnly);
                });

            foreach(var subscription in subscriptions?.Value ?? new List<Subscription>())
            {
                // Delete the subscription
                await _graphClient.Subscriptions[subscription.Id]
                    .DeleteAsync(req =>
                    {
                        req.Options.WithAppOnly(appOnly);
                    });

                // Remove the subscription from the subscription store
                _subscriptionStore.DeleteSubscriptionRecord(subscription.Id!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting existing subscriptions");
        }
    }
}
