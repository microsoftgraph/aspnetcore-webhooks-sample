// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// Subscription record saved in subscription store
/// </summary>
public record SubscriptionRecord
{
    /// <summary>
    /// The ID of the subscription
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The user's ID associated with the subscription
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// The tenant ID of the organization
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// The client state set in the subscription
    /// </summary>
    public string ClientState { get; set; }
}
