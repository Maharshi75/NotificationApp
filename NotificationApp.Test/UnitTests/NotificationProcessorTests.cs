using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using NotificationApp.Core.Services;

namespace NotificationApp.Test.UnitTests
{
    public class NotificationProcessorTests
    {
        private readonly Mock<IRateLimiter> _rateLimiter = new();
        private readonly Mock<IDiscordSender> _sender = new();

        private NotificationProcessor CreateProcessor() =>
            new(_rateLimiter.Object, _sender.Object, Options.Create(new NotificationSettings
            {
                RateLimitPerMinute = 10
            }
        ));

        private static NotificationRequest BuildRequest(string level = "Warning") => new()
        {
            Title = "Test notification",
            Message = "This is a test message",
            Level = level,
        };

        [Fact]
        public async Task MeetsThreshold_WithinLimit_ReturnsSent()
        {
            _rateLimiter.Setup(r => r.TryConsume()).Returns(true);
            var result = await CreateProcessor().ProcessAsync(BuildRequest("Warning"));
            result.Should().Be(ProcessResult.Sent);
        }

        [Fact]
        public async Task AboveThreshold_LimitReached_Returns429()
        {
            _rateLimiter.Setup(r => r.TryConsume()).Returns(false);
            var result = await CreateProcessor().ProcessAsync(BuildRequest("Warning"));
            result.Should().Be(ProcessResult.RateLimitReached);
            _sender.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task InvalidLevel_ReturnsBadRequest()
        {
            var result = await CreateProcessor().ProcessAsync(BuildRequest("NotALevel"));
            result.Should().Be(ProcessResult.InvalidLevel);
            _sender.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessAsync_SenderThrows_ReturnsForwardingFailed()
        {
            _rateLimiter.Setup(r => r.TryConsume()).Returns(true);
            _sender
                .Setup(s => s.SendAsync(It.IsAny<NotificationRecord>(), default))
                .ThrowsAsync(new HttpRequestException("Discord failed"));

            var result = await CreateProcessor().ProcessAsync(BuildRequest("Warning"));

            result.Should().Be(ProcessResult.SendFailed);
        }
    }
}
