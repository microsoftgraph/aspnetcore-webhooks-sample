/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Newtonsoft.Json;

namespace GraphWebhooks_Core.Models
{
    public class NotificationViewModel
    {
        public string JsonPayload { get; set; }
        public NotificationViewModel(object notificationValue)
        {
            JsonPayload = JsonConvert.SerializeObject(notificationValue);
        }
        public NotificationViewModel(string notificationValue)
        {
            JsonPayload = notificationValue;
        }
    }
}
