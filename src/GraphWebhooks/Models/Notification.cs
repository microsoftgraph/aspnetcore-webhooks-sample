// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// Payload sent via SignalR to listening clients
/// </summary>
public class ClientNotification
{
    /// <summary>
    /// The resource that triggered the notification
    /// </summary>
    public object Resource { get; }

    public ClientNotification(object notificationValue)
    {
        Resource = notificationValue;
    }
}
