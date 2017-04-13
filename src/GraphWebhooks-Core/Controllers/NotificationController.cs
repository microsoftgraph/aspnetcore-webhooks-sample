/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Graph;
using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Models;
using GraphWebhooks_Core.SignalR;
using Microsoft.Extensions.Logging;

namespace GraphWebhooks_Core.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ISDKHelper sdkHelper;
        private readonly ISubscriptionStore subscriptionStore;
        private readonly IConnectionManager connectionManager;
        private readonly ILogger logger;

        public NotificationController(ISDKHelper sdkHelper,
                                      ISubscriptionStore subscriptionStore,
                                      IConnectionManager connectionManager,
                                      ILogger<NotificationController> logger)
        {
            this.sdkHelper = sdkHelper;
            this.subscriptionStore = subscriptionStore;
            this.connectionManager = connectionManager;
            this.logger = logger;
        }

        [Authorize]
        public ActionResult LoadView(string id)
        {
            ViewBag.CurrentSubscriptionId = id; // Passing this along so we can delete it later.
            return View("Notification");
        }

        // The notificationUrl endpoint that's registered with the webhook subscription.
        [HttpPost]
        public async Task<ActionResult> Listen()
        {

            // Validate the new subscription by sending the token back to Microsoft Graph.
            // This response is required for each subscription.
            var query = QueryHelpers.ParseQuery(Request.QueryString.ToString());
            if (query.ContainsKey("validationToken"))
            {                
                return Content(query["validationToken"], "plain/text");
            }

            // Parse the received notifications.
            else
            {
                try
                {
                    Dictionary<string, Notification> notifications = new Dictionary<string, Notification>();
                    using (var inputStream = new System.IO.StreamReader(Request.Body))
                    {
                        JObject jsonObject = JObject.Parse(inputStream.ReadToEnd());
                        if (jsonObject != null)
                        {
                            
                            // Notifications are sent in a 'value' array. The array might contain multiple notifications for events that are
                            // registered for the same notification endpoint, and that occur within a short timespan.
                            JArray value = JArray.Parse(jsonObject["value"].ToString());
                            foreach (var notification in value)
                            {
                                Notification current = JsonConvert.DeserializeObject<Notification>(notification.ToString());
                                SubscriptionStore subscription = subscriptionStore.GetSubscriptionInfo(current.SubscriptionId);
                                
                                // Verify the current client state matches the one that was sent.
                                if (current.ClientState == subscription.ClientState)
                                {

                                    // Just keep the latest notification for each resource. No point pulling data more than once.
                                    notifications[current.Resource] = current;
                                }
                            }

                            if (notifications.Count > 0)
                            {

                                // Query for the changed messages. 
                                await GetChangedMessagesAsync(notifications.Values);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"ParsingNotification: { ex.Message }");

                    // TODO: Handle the exception. 
                    // Still return a 202 so the service doesn't resend the notification.
                }
                return new StatusCodeResult(202);
            }
        }

        // Get information about the changed messages and send to browser via SignalR.
        // A production application would typically queue a background job for reliability.
        private async Task GetChangedMessagesAsync(IEnumerable<Notification> notifications)
        {
            List<MessageViewModel> messages = new List<MessageViewModel>();
            foreach (var notification in notifications)
            {
                if (notification.ResourceData.ODataType != "#Microsoft.Graph.Message") continue;

                SubscriptionStore subscription = subscriptionStore.GetSubscriptionInfo(notification.SubscriptionId);

                // Initialize the GraphServiceClient. This sample uses the tenant ID the cache key.
                GraphServiceClient graphClient = sdkHelper.GetAuthenticatedClient(subscription.TenantId);

                MessageRequest request = new MessageRequest(graphClient.BaseUrl + "/" + notification.Resource, graphClient, null);
                try
                {
                    messages.Add(new MessageViewModel(await request.GetAsync(), subscription.UserId));
                }
                catch (ServiceException se)
                {
                    string errorMessage = se.Error.Message;
                    string requestId = se.Error.InnerError.AdditionalData["request-id"].ToString();
                    string requestDate = se.Error.InnerError.AdditionalData["date"].ToString();

                    logger.LogError($"RetrievingMessages: { errorMessage } Request ID: { requestId } Date: { requestDate }");
                }
            }
            
            if (messages.Count > 0)
            {
                NotificationService notificationService = new NotificationService();

                // Clients use the subscribedUserId to filter for messages that belong to the current user. 
                notificationService.SendNotificationToClient(connectionManager, messages);
            }
        }
    }
}