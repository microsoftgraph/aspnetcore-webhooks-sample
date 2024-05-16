// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// Subscription record saved in subscription store.
/// </summary>
public record SubscriptionRecord
{
    /// <summary>
    /// Gets or sets the ID of the subscription.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the user's ID associated with the subscription.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID of the organization.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client state set in the subscription.
    /// </summary>
    public string? ClientState { get; set; }
}
