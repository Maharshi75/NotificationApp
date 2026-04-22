using Microsoft.Extensions.Options;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using System.Collections.Concurrent;

namespace NotificationApp.Core.Services
{
    public sealed class NotificationRateLimiter : IRateLimiter
    {
        private readonly int _limit;
        private readonly ConcurrentQueue<DateTime> _windowTimestamps = new();

        public NotificationRateLimiter(IOptions<NotificationSettings> options)
        {
            _limit = options.Value.RateLimitPerMinute;
        }

        /// <summary>
        /// Attempts to consume one slot in the current 60-second window.
        /// Returns true if the message is allowed, false if the limit is reached.
        /// </summary>
        public bool TryConsume()
        {
            RemoveExpired();

            if (_windowTimestamps.Count >= _limit)
                return false;

            _windowTimestamps.Enqueue(DateTime.UtcNow);

            return true;
        }

        /// <summary>
        /// Removes timestamps that have fallen outside the 60-second window.
        /// ConcurrentQueue is FIFO — oldest timestamps are always at the front.
        /// </summary>
        private void RemoveExpired()
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-60);
            while (_windowTimestamps.TryPeek(out var oldest) && oldest < cutoff)
                _windowTimestamps.TryDequeue(out _);
        }
    }
}
