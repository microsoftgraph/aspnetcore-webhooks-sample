/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using GraphWebhooks_Core.Helpers;

namespace GraphWebhooks_Core.Controllers
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class SubscriptionController : Controller
    {
        private readonly IMemoryCache memoryCache;
        private readonly ISDKHelper sdkHelper;

        public SubscriptionController(IMemoryCache memoryCache, 
                                      ISDKHelper sdkHelper)
        {
            this.memoryCache = memoryCache;
            this.sdkHelper = sdkHelper;
        }

        // Create a subscription.
        // For the Resource, use the `users/user-id` or `users/user-principal-name` path (not `me`) when using application permissions.
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            string tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            string clientState = $"{ tenantId }__{ Guid.NewGuid().ToString() }";

            Subscription newSubscription = new Subscription();
            try
            {

                // Initialize the GraphServiceClient and create a subscription. 
                // This sample passes in the tenant ID to use as a cache key.
                GraphServiceClient graphClient = sdkHelper.GetAuthenticatedClient(tenantId); 
                newSubscription = await graphClient.Subscriptions.Request().AddAsync(new Subscription
                {
                    Resource = $"users/{ userId }/mailFolders('Inbox')/messages",
                    ChangeType = "created",
                    NotificationUrl = Startup.NotificationUrl,
                    ClientState = clientState,
                    //ExpirationDateTime = DateTime.UtcNow + new TimeSpan(0, 0, 4230, 0) // current maximum lifespan for messages
                    ExpirationDateTime = DateTime.UtcNow + new TimeSpan(0, 0, 10, 0)     // shorter duration useful for testing
                });

                // Verify client state, then store the subscription ID and client state to validate incoming notifications.
                if (newSubscription.ClientState == clientState)
                {

                    // This sample temporarily stores the subscription data, but production apps will likely use some method of persistent storage.
                    // This sample also stores the tenant ID to reuse tokens and the user ID to filter messages to display by user.
                    memoryCache.Set("subscriptionId_" + newSubscription.Id,
                        Tuple.Create(newSubscription.ClientState, userId),
                        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24)));
                }
                else
                {
                    ViewBag.Message = "Warning! Mismatched client state.";
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = BuildErrorMessage(e); 
                return View("Error");
            }
            return View("Subscription", newSubscription);
        }

        // Delete a subscription.
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            string tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            try
            {
                // Initialize the GraphServiceClient and delete the subscription.
                GraphServiceClient graphClient = sdkHelper.GetAuthenticatedClient(tenantId);
                await graphClient.Subscriptions[id].Request().DeleteAsync();

                memoryCache.Remove("subscriptionId_" + id);
            }
            catch (Exception e)
            {
                ViewBag.Message = BuildErrorMessage(e);
                return View("Error");
            }
            ViewBag.Message = $"Deleted subscription {id}";
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
