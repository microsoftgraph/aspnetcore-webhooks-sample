/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Helpers.Interfaces;
using GraphWebhooks_Core.Infrastructure;
using GraphWebhooks_Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionStore subscriptionStore;
        private readonly ITokenAcquisition tokenAcquisition;
        private readonly KeyVaultManager keyVaultManager;
        private readonly IOptions<SubscriptionOptions> subscriptionOptions;
        private readonly IOptions<AppSettings> appSettings;

        public SubscriptionController(ISubscriptionStore subscriptionStore,
                                      ITokenAcquisition tokenAcquisition,
                                      KeyVaultManager keyVaultManager,
                                      IOptions<SubscriptionOptions> subscriptionOptions,
                                      IOptions<AppSettings> appSettings)
        {
            this.subscriptionStore = subscriptionStore;
            this.tokenAcquisition = tokenAcquisition;
            this.keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
            this.subscriptionOptions = subscriptionOptions ?? throw new ArgumentNullException(nameof(subscriptionOptions));
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        // Create a subscription with a delegated context
        [Authorize]
        [AuthorizeForScopes(ScopeKeySection = "SubscriptionSettings:Scope")]
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            string tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            string clientState = Guid.NewGuid().ToString();

            // Initialize the GraphServiceClient
            var graphClient = await GraphServiceClientFactory.GetAuthenticatedGraphClient(appSettings.Value.GraphApiUrl, async() =>
            {
                string result = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { subscriptionOptions.Value.Scope });
                return result;
            });

            try
            {
                // Create a subscription.
                var newSubscription = await CreateSubscription(userId, tenantId, clientState, graphClient).ConfigureAwait(false);
                return View("Subscription", newSubscription);
            }
            catch (Exception e)
            {

                // If a tenant admin hasn't granted consent, this operation returns an Unauthorized error.
                // This sample caches the initial unauthorized token, so you'll need to start a new browser session.
                ViewBag.Message = BuildErrorMessage(e);
                return View("Error");
            }
        }

        // Create a subscription with an app only context
        public async Task<IActionResult> CreateAppOnly()
        {
            string clientState = Guid.NewGuid().ToString();

            // Initialize the GraphServiceClient
            var graphClient = await GraphServiceClientFactory.GetAuthenticatedGraphClient(appSettings.Value.GraphApiUrl, async() =>
            {
                return await tokenAcquisition.AcquireTokenForAppAsync(new string[] { $"{appSettings.Value.GraphApiUrl}/.default" });
            });

            try
            {
                // Create a subscription.
                var newSubscription = await CreateSubscription(string.Empty, string.Empty, clientState, graphClient).ConfigureAwait(false);
                return View("Subscription", newSubscription);
            }
            catch (Exception e)
            {

                // If a tenant admin hasn't granted consent, this operation returns an Unauthorized error.
                // This sample caches the initial unauthorized token, so you'll need to start a new browser session.
                ViewBag.Message = BuildErrorMessage(e);
                return View("Error");
            }
        }

        private async Task<Subscription> CreateSubscription(string userId, string tenantId, string clientState, GraphServiceClient graphClient)
        {
            string encryptionCertificate = subscriptionOptions.Value.IncludeResourceData ? await keyVaultManager.GetEncryptionCertificate().ConfigureAwait(false) : null;
            string encryptionCertificateId = subscriptionOptions.Value.IncludeResourceData ? await keyVaultManager.GetEncryptionCertificateId().ConfigureAwait(false) : null;

            var newSubscription = await graphClient.Subscriptions.Request().AddAsync(new Subscription
            {
                Resource = subscriptionOptions.Value.Resource,
                ChangeType = subscriptionOptions.Value.ChangeType,
                NotificationUrl = subscriptionOptions.Value.NotificationUrl,
                ClientState = clientState,
                ExpirationDateTime = DateTime.UtcNow + new TimeSpan(0, 0, 15, 0),     // 4230 minutes is the current max lifetime, shorter duration useful for testing
                EncryptionCertificate = encryptionCertificate,
                EncryptionCertificateId = encryptionCertificateId,
                IncludeResourceData = subscriptionOptions.Value.IncludeResourceData
            });


            // This sample temporarily stores the subscription data, but production apps will likely use some method of persistent storage.
            // This sample stores the client state to validate the subscription, the tenant ID to reuse tokens, and the user ID to filter
            // messages to display by user.
            subscriptionStore.SaveSubscriptionInfo(newSubscription.Id,
                newSubscription.ClientState,
                userId,
                tenantId);

            return newSubscription;
        }

        // Delete a subscription with delegated context
        [HttpPost]
        [Authorize]
        [AuthorizeForScopes(ScopeKeySection = "SubscriptionSettings:Scope")]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (!string.IsNullOrEmpty(id))
            {

                // Initialize the GraphServiceClient and delete the subscription.
                var graphClient = await GraphServiceClientFactory.GetAuthenticatedGraphClient(appSettings.Value.GraphApiUrl, async() =>
                {
                    return await tokenAcquisition.GetAccessTokenForUserAsync(new[] { subscriptionOptions.Value.Scope });
                });


                try
                {
                    await graphClient.Subscriptions[id].Request().DeleteAsync();
                }
                catch (Exception e)
                {
                    ViewBag.Message = BuildErrorMessage(e);
                    return View("Error");
                }
                ViewBag.Message = $"Deleted subscription {id}";
            }
            return View("Subscription");
        }
        // Delete a subscription with app only context
        [HttpPost]
        public async Task<IActionResult> DeleteAppOnly(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {

                // Initialize the GraphServiceClient and delete the subscription.
                var graphClient = await GraphServiceClientFactory.GetAuthenticatedGraphClient(appSettings.Value.GraphApiUrl, async() =>
                {
                    return await tokenAcquisition.AcquireTokenForAppAsync(new string[] { $"{appSettings.Value.GraphApiUrl}/.default" });
                });


                try
                {
                    await graphClient.Subscriptions[id].Request().DeleteAsync();
                }
                catch (Exception e)
                {
                    ViewBag.Message = BuildErrorMessage(e);
                    return View("Error");
                }
                ViewBag.Message = $"Deleted subscription {id}";
            }
            return View("Subscription");
        }

        private string BuildErrorMessage(Exception e)
        {
            string message = e.Message;
            if (e is ServiceException)
            {
                ServiceException se = e as ServiceException;
                string requestId = se.Error.InnerError.AdditionalData["request-id"].ToString();
                string requestDate = se.Error.InnerError.AdditionalData["date"].ToString();
                message = $"{ se.Error.Message } Request ID: { requestId } Date: { requestDate }";
            }
            return message;
        }
    }
}
