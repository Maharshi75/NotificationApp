using Microsoft.Extensions.Options;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;

namespace NotificationApp.Core.Services
{
    public class NotificationProcessor
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly IDiscordSender _sender;
        private readonly NotificationSettings _settings;

        public NotificationProcessor(IRateLimiter rateLimiter, IDiscordSender sender, IOptions<NotificationSettings> options)
        {
            _rateLimiter = rateLimiter;
            _sender = sender;
            _settings = options.Value;
        }

        public async Task<ProcessResult> ProcessAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            var level = NotificationLevelExtensions.ParseLevel(request.Level);
            if (level is null)
                return ProcessResult.InvalidLevel;

            var record = NotificationRecord.FromRequest(request, level.Value);

            if (!record.Level.ShouldForward(_settings.ForwardThreshold))
                return ProcessResult.LoggedOnly;

            if (!_rateLimiter.TryConsume())
                return ProcessResult.RateLimitReached;

            try
            {
                await _sender.SendAsync(record, cancellationToken);
                return ProcessResult.Sent;
            }
            catch (HttpRequestException)
            {
                return ProcessResult.SendFailed;
            }
        }
    }

    /// <summary>
    /// Describes the outcome of processing a single notification.
    /// </summary>
    public enum ProcessResult
    {
        LoggedOnly, //Logged if level is within the threshold

        Sent, //Successfully sent to the external sender.

        RateLimitReached, //10/min limit reached — caller should respond with 429.

        InvalidLevel, //Level string was not recognised — caller should respond with 400.

        SendFailed  // Discord rejected the request
    }
}
