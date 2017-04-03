using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
