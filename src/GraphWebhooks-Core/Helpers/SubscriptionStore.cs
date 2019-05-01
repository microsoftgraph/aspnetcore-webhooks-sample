/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using System;
using GraphWebhooks_Core.Helpers.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GraphWebhooks_Core.Helpers
{
    public class SubscriptionStore : ISubscriptionStore
    {
        private readonly IMemoryCache memoryCache;

        public string SubscriptionId { get; set; }
        public string ClientState { get; set; }
        public string UserId { get; set; }
        public string TenantId { get; set; }

        // Dependency injection constructor.
        public SubscriptionStore(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        private SubscriptionStore(string subscriptionId, Tuple<string, string, string> parameters)
        {
            SubscriptionId = subscriptionId;
            ClientState = parameters.Item1;
            UserId = parameters.Item2;
            TenantId = parameters.Item3;
        }

        // This sample temporarily stores the current subscription ID, client state, user object ID, and tenant ID. 
        // This info is required so the NotificationController can validate the subscription, retrieve an access token from the cache, and filter 
        // the messages this sample displays to the user.
        // Production apps typically use some method of persistent storage.
        public void SaveSubscriptionInfo(string subscriptionId, string clientState, string userId, string tenantId)
        {
            memoryCache.Set("subscriptionId_" + subscriptionId,
                Tuple.Create(clientState, userId, tenantId),
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24)));
        }

        public SubscriptionStore GetSubscriptionInfo(string subscriptionId)
        {
            Tuple<string, string, string> subscriptionParams = memoryCache.Get("subscriptionId_" + subscriptionId) as Tuple<string, string, string>;
            return new SubscriptionStore(subscriptionId, subscriptionParams);
        }
    }    
}