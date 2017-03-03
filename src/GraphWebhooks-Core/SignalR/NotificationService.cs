/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.Graph;

namespace GraphWebhooks_Core.SignalR
{
    public class NotificationService : PersistentConnection
    {
        public void SendNotificationToClient(IConnectionManager connectionManager, List<Message> messages, string userId)
        {
            var hubContext = connectionManager.GetHubContext<NotificationHub>();
            if (hubContext != null)
            {
                hubContext.Clients.All.showNotification(messages, userId);
            }
        }
    }
}