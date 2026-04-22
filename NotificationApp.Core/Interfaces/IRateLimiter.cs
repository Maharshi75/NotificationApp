namespace NotificationApp.Core.Interfaces
{
    public interface IRateLimiter
    {
        /// <summary>
        /// Attempts to consume one slot from the rate limit window.
        /// Returns true if the message is allowed to be sent immediately.
        /// Returns false if the limit has been reached for the current window.
        /// </summary>
        public bool TryConsume();
    }
}
