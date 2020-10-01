using System;

namespace GraphWebhooks_Core.Infrastructure
{
    public class DownstreamApiSettings
    {
        public string BaseUrl { get; set; }
        public string BaseUrlWithoutVersion
        {
            get
            {
                var uri = new Uri(BaseUrl);
                return $"{uri.Scheme}://{uri.Host}/";
            }
        }
    }
}
