/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

namespace GraphWebhooks_Core.Infrastructure
{
    public class AppSettings
    {
        public string GraphApiUrl { get; set; }        
        public string NotificationUrl { get; set; }
        public string BaseRedirectUrl { get; set; }
        public string AADInstance { get; internal set; }
        public string AppId { get; internal set; }
        public string AppSecret { get; internal set; }
        public string GraphResourceId { get; internal set; }
    }
}