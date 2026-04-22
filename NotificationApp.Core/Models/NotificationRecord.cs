namespace NotificationApp.Core.Models
{
    public record NotificationRecord
    {
        public Guid Id { get; init; }
        public NotificationLevel Level { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Source { get; init; } = "Unknown";
        public DateTime Timestamp { get; init; }

        public static NotificationRecord FromRequest(NotificationRequest request, NotificationLevel level)
        => new()
        {
            Id = Guid.NewGuid(),
            Level = level,
            Title = request.Title,
            Message = request.Message,
            Source = request.Source ?? "Unknown",
            Timestamp = request.Timestamp
        };
    }
}
