// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using GraphWebhooks.Models;
using GraphWebhooks.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;

namespace GraphWebhooks.Controllers;

/// <summary>
/// Implements subscription management endpoints.
/// </summary>
public class WatchController : Controller
{
    private readonly GraphServiceClient graphClient;
    private readonly SubscriptionStore subscriptionStore;
    private readonly CertificateService certificateService;
    private readonly ILogger<WatchController> logger;
    private readonly string notificationHost;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchController"/> class.
    /// </summary>
    /// <param name="graphClient">The <see cref="GraphServiceClient"/>.</param>
    /// <param name="subscriptionStore">The subscription store.</param>
    /// <param name="certificateService">The certificate service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The app configuration.</param>
    /// <exception cref="ArgumentException">Thrown if any parameter is null.</exception>
    public WatchController(
        GraphServiceClient graphClient,
        SubscriptionStore subscriptionStore,
        CertificateService certificateService,
        ILogger<WatchController> logger,
        IConfiguration configuration)
    {
        this.graphClient = graphClient ??
            throw new ArgumentException("GraphServiceClient cannot be null", nameof(graphClient));
        this.subscriptionStore = subscriptionStore ??
            throw new ArgumentException("SubscriptionStore cannot be null", nameof(subscriptionStore));
        this.certificateService = certificateService ??
            throw new ArgumentException("CertificateService cannot be null", nameof(certificateService));
        this.logger = logger ??
            throw new ArgumentException("ILogger cannot be null", nameof(logger));
        _ = configuration ??
            throw new ArgumentException("IConfiguration cannot be null", nameof(configuration));

        notificationHost = configuration.GetValue<string>("NotificationHost") is string hostValue &&
            !string.IsNullOrEmpty(hostValue) && !hostValue.Equals("YOUR_NGROK_PROXY", StringComparison.OrdinalIgnoreCase) ? hostValue :
            throw new ArgumentException("You must configure NotificationHost in appsettings.json");
    }

    /// <summary>
    /// GET /watch/delegated
    /// Creates a new subscription to the authenticated user's inbox and
    /// displays a page that updates with each received notification.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    [AuthorizeForScopes(ScopeKeySection = "GraphScopes")]
    public async Task<IActionResult> Delegated()
    {
        try
        {
            // Delete any existing subscriptions for the user
            await DeleteAllSubscriptions(false);

            // Get the user's ID and tenant ID from the user's identity
            var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            logger.LogInformation("Authenticated user ID {userId}", userId);
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            // Get the user from Microsoft Graph
            var user = await graphClient.Me.GetAsync(req =>
            {
                req.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
            });

            if (user == null)
            {
                logger.LogWarning("Could not retrieve authenticated user.");
                return View().WithError("Could not retrieve authenticated user.");
            }

            logger.LogInformation(
                "Authenticated user: {displayName} ({email})",
                user.DisplayName,
                user.Mail ?? user.UserPrincipalName);

            // Add the user's display name and email address to the user's
            // identity.
            User.AddUserGraphInfo(user);

            // Create the subscription
            var subscription = new Subscription
            {
                ChangeType = "created",
                NotificationUrl = $"{notificationHost}/listen",
                LifecycleNotificationUrl = $"{notificationHost}/lifecycle",
                Resource = "me/mailfolders/inbox/messages",
                ClientState = Guid.NewGuid().ToString(),
                IncludeResourceData = false,

                // Subscription only lasts for one hour
                ExpirationDateTime = DateTimeOffset.UtcNow.AddHours(1),
            };

            var newSubscription = await graphClient.Subscriptions
                .PostAsync(subscription);

            if (newSubscription == null)
            {
                return View().WithError("No subscription was returned.");
            }

            // Add the subscription to the subscription store
            subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
            {
                Id = newSubscription.Id,
                UserId = userId,
                TenantId = tenantId,
                ClientState = newSubscription.ClientState,
            });

            return View(newSubscription).WithSuccess("Subscription created");
        }
        catch (Exception ex)
        {
            // Throw MicrosoftIdentityWebChallengeUserException to allow
            // Microsoft.Identity.Web to challenge the user for re-auth or consent
            if (ex.InnerException is MicrosoftIdentityWebChallengeUserException)
            {
                throw;
            }

            // Otherwise display the error
            return View().WithError(
                $"Error creating subscription: {ex.Message}",
                ex.ToString());
        }
    }

    /// <summary>
    /// GET /watch/apponly
    /// Creates a new subscription to all Teams channel messages and
    /// displays a page that updates with each received notification.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/>.</returns>
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
            var encryptionCertificate = await certificateService.GetEncryptionCertificate();

            // Create the subscription
            var subscription = new Subscription
            {
                ChangeType = "created",
                NotificationUrl = $"{notificationHost}/listen",
                LifecycleNotificationUrl = $"{notificationHost}/lifecycle",
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

            var newSubscription = await graphClient.Subscriptions.PostAsync(subscription, req =>
            {
                req.Options.WithAppOnly();
            });

            if (newSubscription == null)
            {
                return RedirectToAction("Index", "Home")
                    .WithError("No subscription was returned.");
            }

            // Add the subscription to the subscription store
            subscriptionStore.SaveSubscriptionRecord(new SubscriptionRecord
            {
                Id = newSubscription.Id,
                UserId = "APP-ONLY",
                TenantId = tenantId,
                ClientState = newSubscription.ClientState,
            });

            return View(newSubscription).WithSuccess("Subscription created");
        }
        catch (Exception ex)
        {
            return RedirectToAction("Index", "Home")
                .WithError(
                    $"Error creating subscription: {ex.Message}",
                    ex.ToString());
        }
    }

    /// <summary>
    /// GET /watch/unsubscribe
    /// Deletes the user's inbox subscription and signs the user out.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    public async Task<IActionResult> Unsubscribe(string subscriptionId)
    {
        if (string.IsNullOrEmpty(subscriptionId))
        {
            return RedirectToAction("Index", "Home")
                .WithError("No subscription ID specified");
        }

        try
        {
            var subscription = subscriptionStore.GetSubscriptionRecord(subscriptionId);

            if (subscription != null)
            {
                var appOnly = subscription.UserId == "APP-ONLY";

                // To unsubscribe, just delete the subscription
                await graphClient.Subscriptions[subscriptionId]
                    .DeleteAsync(req =>
                    {
                        req.Options.WithAppOnly(appOnly);
                    });

                // Remove the subscription from the subscription store
                subscriptionStore.DeleteSubscriptionRecord(subscriptionId);
            }
        }
        catch (Exception ex)
        {
            // Throw MicrosoftIdentityWebChallengeUserException to allow
            // Microsoft.Identity.Web to challenge the user for re-auth or consent
            if (ex.InnerException is MicrosoftIdentityWebChallengeUserException)
            {
                throw;
            }

            // Otherwise log the error
            logger.LogError(ex, "Error deleting subscription");
        }

        // Redirect to Microsoft.Identity.Web's signout page
        return RedirectToAction("SignOut", "Account", new { area = "MicrosoftIdentity" });
    }

    /// <summary>
    /// Deletes all current subscriptions.
    /// </summary>
    /// <param name="appOnly">If true, all app-only subscriptions are removed. If false, all user subscriptions are removed.</param>
    private async Task DeleteAllSubscriptions(bool appOnly)
    {
        try
        {
            // Get all current subscriptions
            var subscriptions = await graphClient.Subscriptions
                .GetAsync(req =>
                {
                    req.Options.WithAppOnly(appOnly);
                });

            foreach (var subscription in subscriptions?.Value ?? [])
            {
                // Delete the subscription
                await graphClient.Subscriptions[subscription.Id]
                    .DeleteAsync(req =>
                    {
                        req.Options.WithAppOnly(appOnly);
                    });

                // Remove the subscription from the subscription store
                subscriptionStore.DeleteSubscriptionRecord(subscription.Id!);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting existing subscriptions");
        }
    }
}
