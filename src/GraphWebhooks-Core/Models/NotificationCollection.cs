/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Models
{
    public class NotificationCollection
    {
        [JsonProperty(PropertyName = "value")]
        public List<Notification> Value { get; set; }

        [JsonProperty(PropertyName = "validationTokens")]
        public List<string> ValidationTokens { get; set; }
    }
}
