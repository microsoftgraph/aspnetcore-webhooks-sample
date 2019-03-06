/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using GraphWebhooks_Core.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using Microsoft.Identity.Web.Client;
using GraphWebhooks_Core.Infrastructure;

namespace GraphWebhooks_Core.Controllers
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionStore subscriptionStore;
        private readonly AppSettings appSettings;
        readonly ITokenAcquisition tokenAcquisition;

        public SubscriptionController(ISubscriptionStore subscriptionStore,
                                      IOptions<AppSettings> optionsAccessor,
                                      ITokenAcquisition tokenAcquisition)
        {            
            this.subscriptionStore = subscriptionStore;
            appSettings = optionsAccessor.Value;
            this.tokenAcquisition = tokenAcquisition;
        }

        // Create a subscription.
        [MsalUiRequiredExceptionFilter(Scopes = new[] { Infrastructure.Constants.ScopeMailRead })]
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            string tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            string clientState = Guid.NewGuid().ToString();

            // Fetch the access token
            string accessToken = await tokenAcquisition.GetAccessTokenOnBehalfOfUser(
                    HttpContext, new[] { Infrastructure.Constants.ScopeMailRead });

            Subscription newSubscription = new Subscription();
            try
            {
                // Initialize the GraphServiceClient.                
                var graphClient = GraphServiceClientFactory.GetAuthenticatedGraphClient(accessToken);

                // Create a subscription.
                // The `Resource` property targets the `users/{user-id}` or `users/{user-principal-name}` path (not `me`) when using application permissions.
                // The NotificationUrl requires the `https` protocol and supports custom query parameters.

                newSubscription = await graphClient.Subscriptions.Request().AddAsync(new Subscription
                {
                    Resource = $"users/{userId}/mailFolders('Inbox')/messages",
                    ChangeType = "created",
                    NotificationUrl = appSettings.NotificationUrl,
                    ClientState = clientState,
                    //ExpirationDateTime = DateTime.UtcNow + new TimeSpan(0, 0, 4230, 0) // current maximum lifespan for messages
                    ExpirationDateTime = DateTime.UtcNow + new TimeSpan(0, 0, 15, 0)     // shorter duration useful for testing
                });
                

                // Verify client state, then store the subscription ID and client state to validate incoming notifications.
                if (newSubscription.ClientState == clientState)
                {

                    // This sample temporarily stores the subscription data, but production apps will likely use some method of persistent storage.
                    // This sample stores the client state to validate the subscription, the tenant ID to reuse tokens, and the user ID to filter
                    // messages to display by user.
                    subscriptionStore.SaveSubscriptionInfo(newSubscription.Id,
                        newSubscription.ClientState,
                        userId,
                        tenantId);
                }
                else
                {
                    ViewBag.Message = "Warning! Mismatched client state.";
                }
            }
            catch (Exception e)
            {

                // If a tenant admin hasn't granted consent, this operation returns an Unauthorized error.
                // This sample caches the initial unauthorized token, so you'll need to start a new browser session.
                ViewBag.Message = BuildErrorMessage(e);
                return View("Error");
            }

            return View("Subscription", newSubscription);
        }

        // Delete a subscription.
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {              
                // Fetch the access token
                string accessToken = await tokenAcquisition.GetAccessTokenOnBehalfOfUser(
                    HttpContext, new[] { Infrastructure.Constants.ScopeMailRead });

                try
                {
                    // Initialize the GraphServiceClient and delete the subscription.
                    var graphClient = GraphServiceClientFactory.GetAuthenticatedGraphClient(accessToken);

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
