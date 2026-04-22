using Microsoft.Extensions.Logging;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;

namespace NotificationApp.Core.Services
{
    public sealed class MockDiscordSender : IDiscordSender
    {
        private readonly ILogger<MockDiscordSender> _logger;

        public MockDiscordSender(ILogger<MockDiscordSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MOCK DISCORD] Id={Id} | Level={Level} | Source={Source} | Title={Title} | Message={Message} | Timestamp={Timestamp:O}",
                record.Id,
                record.Level,
                record.Source,
                record.Title,
                record.Message,
                record.Timestamp);

            return Task.CompletedTask;
        }
    }
}
