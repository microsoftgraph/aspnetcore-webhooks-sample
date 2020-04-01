/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.Graph;
using System.Collections.Generic;
using System.Linq;

namespace GraphWebhooks_Core.Models
{
    public class MessageViewModel
    {
        public string From { get; set; }
        public List<string> To { get; set; } = new List<string>();
        public string Subject { get; set; }
        public string SentDateTime { get; set; }
        public string SubscriberId { get; set; }

        public MessageViewModel(Message message, string subscribedUserId)
        {
            From = message?.From?.EmailAddress?.Address;
            Subject = message?.Subject;
            SentDateTime = message?.SentDateTime.HasValue ?? false ? message?.SentDateTime.Value.ToString() : string.Empty;
            To.AddRange(message?.ToRecipients.Select(x => x.EmailAddress.Address));
            SubscriberId = subscribedUserId;
        }
    }
}
