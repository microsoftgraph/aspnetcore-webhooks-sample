/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

namespace GraphWebhooks_Core.Helpers
{
    public class AppSettings
    {
        public string AADInstance { get; set; }
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string BaseRedirectUri { get; set; }
        public string CallbackPath { get; set; }
        public string GraphResourceId { get; set; }
        public string NotificationUrl { get; set; }
    }
}