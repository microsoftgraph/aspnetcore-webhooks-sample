/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using GraphWebhooks_Core.Models;

namespace GraphWebhooks_Core.SignalR
{
    public class NotificationService
    {
        public async Task SendNotificationToClient(IHubContext<NotificationHub> hubContext, List<NotificationViewModel> messages)
        {
            await hubContext.Clients.All.SendAsync("showNotification", messages);
        }
    }
}