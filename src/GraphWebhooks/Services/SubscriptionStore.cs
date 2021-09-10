// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using GraphWebhooks.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GraphWebhooks.Services
{
    public class SubscriptionStore
    {
        private readonly IMemoryCache _cache;

        public SubscriptionStore(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        public void SaveSubscriptionRecord(SubscriptionRecord record)
        {
            var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(2));
            _cache.Set(record.Id, record, options);
        }

        public SubscriptionRecord GetSubscriptionRecord(string subscriptionId)
        {
            _cache.TryGetValue<SubscriptionRecord>(subscriptionId, out var record);
            return record;
        }

        public void DeleteSubscriptionRecord(string subscriptionId)
        {
            _cache.Remove(subscriptionId);
        }
    }
}
