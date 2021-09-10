// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GraphWebhooks.Models
{
    public class SubscriptionRecord
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string TenantId { get; set; }
        public string ClientState { get; set; }
    }
}
