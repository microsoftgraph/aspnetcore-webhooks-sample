/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.Graph;

namespace GraphWebhooks_Core.Models
{
    public class MessageViewModel
    {
        public Message Message { get; set; }
        public string SubscriberId { get; set; }

        public MessageViewModel(Message message, string subscribedUserId)
        {
            Message = message;
            SubscriberId = subscribedUserId;
        }
    }
}
