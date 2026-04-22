namespace NotificationApp.Core.Models
{
    public class NotificationSettings
    {
        public const string SectionName = "NotificationSettings";
        public NotificationLevel ForwardThreshold { get; init; } = NotificationLevel.Warning;
        public int RateLimitPerMinute { get; init; } = 10;
        public DiscordSettings Discord { get; init; } = new();

    }

    public class DiscordSettings
    {
        public string WebhookUrl { get; init; } = string.Empty;
        public string Username { get; init; } = "Notification Bot";
        public string? AvatarUrl { get; init; }
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(WebhookUrl) &&
                WebhookUrl.StartsWith("https://discord.com/api/webhooks/",
                    StringComparison.OrdinalIgnoreCase);

    }
}
