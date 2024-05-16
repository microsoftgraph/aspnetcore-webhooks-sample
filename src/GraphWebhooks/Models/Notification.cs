// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// Payload sent via SignalR to listening clients
/// </summary>
public class ClientNotification(object notificationValue)
{
    /// <summary>
    /// The resource that triggered the notification
    /// </summary>
    public object Resource { get; } = notificationValue;
}
