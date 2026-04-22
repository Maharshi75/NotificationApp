namespace NotificationApp.Core.Models
{
    public static class NotificationLevelExtensions
    {
        /// <summary>
        /// Parses a case-insensitive string from the HTTP payload into a NotificationLevel.
        /// Returns null if the value is unrecognised, so the controller can return 400.
        /// </summary>
        public static NotificationLevel? ParseLevel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<NotificationLevel>(value, ignoreCase: true, out var result)
                ? result
                : null;
        }

        /// <summary>
        /// Returns true if this level meets or exceeds the configured forward threshold.
        /// </summary>
        public static bool ShouldForward(this NotificationLevel level, NotificationLevel threshold) => level >= threshold;
    }
}
