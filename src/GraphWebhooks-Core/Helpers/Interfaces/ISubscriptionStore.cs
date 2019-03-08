/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

namespace GraphWebhooks_Core.Helpers.Interfaces
{
    public interface ISubscriptionStore
    {
        void SaveSubscriptionInfo(string subscriptionId, string clientState, string userId, string tenantId);

        SubscriptionStore GetSubscriptionInfo(string subscriptionId);
    }
}
