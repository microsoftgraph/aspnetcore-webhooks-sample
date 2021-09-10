// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;

namespace GraphWebhooks.Models
{
    public class ClientNotification
    {
        public object Resource { get; }

        public ClientNotification(object notificationValue)
        {
            Resource = notificationValue;
        }
    }
}
