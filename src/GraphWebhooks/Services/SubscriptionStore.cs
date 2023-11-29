// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using GraphWebhooks.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GraphWebhooks.Services;

/// <summary>
/// Implements an in-memory store of subscriptions
/// </summary>
public class SubscriptionStore
{
    private readonly IMemoryCache _cache;

    public SubscriptionStore(IMemoryCache memoryCache)
    {
        _cache = memoryCache ?? throw new ArgumentException(nameof(memoryCache));
    }

    /// <summary>
    /// Add a subscription record to the store
    /// </summary>
    /// <param name="record">The subscription to add</param>
    public void SaveSubscriptionRecord(SubscriptionRecord record)
    {
        if (string.IsNullOrEmpty(record.Id)) throw new Exception("ID of record cannot be empty");
        var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(2));
        _cache.Set(record.Id, record, options);
    }

    /// <summary>
    /// Get a subscription record
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <returns>The subscription record if found, null if not</returns>
    public SubscriptionRecord? GetSubscriptionRecord(string subscriptionId)
    {
        _cache.TryGetValue<SubscriptionRecord>(subscriptionId, out var record);
        return record;
    }

    /// <summary>
    /// Delete a subscription record
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    public void DeleteSubscriptionRecord(string subscriptionId)
    {
        _cache.Remove(subscriptionId);
    }
}
