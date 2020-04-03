namespace GraphWebhooks_Core.Models
{
    public class SubscriptionOptions
    {
        public bool IncludeResourceData { get; set; }
        public string NotificationUrl { get; set; }
        public string Resource { get; set; }
        public string ChangeType { get; set; }
        public string Scope { get; set; }
    }
}
