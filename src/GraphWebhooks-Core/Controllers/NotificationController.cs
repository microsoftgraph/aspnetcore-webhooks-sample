/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Models;
using GraphWebhooks_Core.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using GraphWebhooks_Core.Helpers.Interfaces;
using System.Linq;
using Microsoft.Extensions.Options;

namespace GraphWebhooks_Core.Controllers
{
    
    public class NotificationController : Controller
    {
        private readonly ISubscriptionStore subscriptionStore;
        private readonly IHubContext<NotificationHub> notificationHub;
        private readonly ILogger logger;
        readonly ITokenAcquisition tokenAcquisition;
        private readonly IOptions<MicrosoftIdentityOptions> identityOptions;
        private readonly KeyVaultManager keyVaultManager;

        public NotificationController(ISubscriptionStore subscriptionStore,
                                      IHubContext<NotificationHub> notificationHub,
                                      ILogger<NotificationController> logger,
                                      ITokenAcquisition tokenAcquisition,
                                      IOptions<MicrosoftIdentityOptions> identityOptions,
                                      KeyVaultManager keyVaultManager)
        {
            this.subscriptionStore = subscriptionStore;
            this.notificationHub = notificationHub;
            this.logger = logger;
            this.tokenAcquisition = tokenAcquisition;
            this.identityOptions = identityOptions ?? throw new ArgumentNullException(nameof(identityOptions));
            this.keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
        }

		[Authorize]
		public ActionResult LoadView(string id)
        {
            ViewBag.CurrentSubscriptionId = id; // Passing this along so we can delete it later.
            return View("Notification");
        }

        // The notificationUrl endpoint that's registered with the webhook subscription.
        [HttpPost]       
        public async Task<IActionResult> Listen([FromQuery]string validationToken = null)
        {
            if(string.IsNullOrEmpty(validationToken))
            {
                try
                {
                    // Parse the received notifications.
                    Dictionary<string, Notification> plainNotifications = new Dictionary<string, Notification>();
                    using var inputStream = new System.IO.StreamReader(Request.Body);
                    var collection = JsonConvert.DeserializeObject<NotificationCollection>(await inputStream.ReadToEndAsync());
                    foreach (var notification in collection.Value.Where(x => x.EncryptedContent == null))
                    {
                        SubscriptionStore subscription = subscriptionStore.GetSubscriptionInfo(notification.SubscriptionId);

                        // Verify the current client state matches the one that was sent.
                        if (notification.ClientState == subscription.ClientState)
                        {
                            // Just keep the latest notification for each resource. No point pulling data more than once.
                            plainNotifications[notification.Resource] = notification;
                        }//TODO return bad request if state is wrong?
                    }

                    if (plainNotifications.Count > 0)
                    {
                        // Query for the changed messages. 
                        await GetChangedMessagesAsync(plainNotifications.Values);
                    }

                    if (collection.ValidationTokens != null && collection.ValidationTokens.Any())
                    { // we're getting notifications with resource data and we should validate tokens and decrypt data
                        TokenValidator tokenValidator = new TokenValidator(identityOptions.Value.TenantId, new[] { identityOptions.Value.ClientId });
                        bool areValidationTokensValid = (await Task.WhenAll(
                            collection.ValidationTokens.Select(x => tokenValidator.ValidateToken(x))).ConfigureAwait(false))
                            .Aggregate((x, y) => x && y);
                        if (areValidationTokensValid)
                        {
                            foreach (var notificationItem in collection.Value.Where(x => x.EncryptedContent != null))
                            {
                                string decryptedpublisherNotification =
                                Decryptor.Decrypt(
                                    notificationItem.EncryptedContent.Data,
                                    notificationItem.EncryptedContent.DataKey,
                                    notificationItem.EncryptedContent.DataSignature,
                                    await keyVaultManager.GetDecryptionCertificate().ConfigureAwait(false));

                                Dictionary<string, object> resourceDataObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedpublisherNotification);
                                Console.WriteLine($"Decrypted Notification: {decryptedpublisherNotification}");
                            }
                            return Accepted();
                        }
                        else
                        {
                            return Unauthorized("Token Validation failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"ParsingNotification: { ex.Message }");

                    // TODO: Handle the exception. 
                    // Still return a 202 so the service doesn't resend the notification.
                }
                return Accepted();
            }
            else
            {
                // Validate the new subscription by sending the token back to Microsoft Graph.
                // This response is required for each subscription.
                return Content(validationToken);
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
                                
                // Set the claims for ObjectIdentifier and TenantId, and              
                // use the above claims for the current HttpContext
                HttpContext.User = ClaimsPrincipalFactory.FromTenantIdAndObjectId(subscription.TenantId, subscription.UserId);

                // Initialize the GraphServiceClient. 
                var graphClient = await GraphServiceClientFactory.GetAuthenticatedGraphClient(async () =>
                {
                    string result = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { Infrastructure.Constants.ScopeMailRead });
                    return result;
                });

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
                                    await notificationService.SendNotificationToClient(notificationHub, messages);                
            }
        }
    }
}