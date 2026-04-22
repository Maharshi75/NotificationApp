using FluentAssertions;
using Microsoft.Extensions.Options;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using NotificationApp.Core.Services;

namespace NotificationApp.Test.UnitTests
{
    public class SlidingWindowRateLimiterTests
    {
        private static IRateLimiter CreateLimiter(int limit = 3) =>
            new NotificationRateLimiter(Options.Create(new NotificationSettings
            {
                RateLimitPerMinute = limit
            }));

        [Fact]
        public void WithinLimit_ReturnsTrue()
        {
            var limiter = CreateLimiter();
            limiter.TryConsume().Should().BeTrue();
        }

        [Fact]
        public void LimitExceeded_ReturnsFalse()
        {
            var limiter = CreateLimiter(limit: 2);
            limiter.TryConsume();
            limiter.TryConsume();
            limiter.TryConsume().Should().BeFalse();
        }
    }
}
