using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using System.Net.Http.Json;

namespace NotificationApp.Core.Services
{
    public class DiscordSender : IDiscordSender
    {
        private readonly HttpClient _httpClient;
        private readonly DiscordSettings _settings;
        private readonly ILogger<DiscordSender> _logger;

        // Colour codes for Discord embed sidebar by level
        private static readonly Dictionary<NotificationLevel, int> LevelColours = new()
        {
            { NotificationLevel.Info,     0x3498DB }, // blue
            { NotificationLevel.Warning,  0xF39C12 }, // orange
            { NotificationLevel.Error,    0xE74C3C }, // red
            { NotificationLevel.Critical, 0x8E44AD }  // purple
        };

        public DiscordSender(IHttpClientFactory httpClientFactory, IOptions<NotificationSettings> options, ILogger<DiscordSender> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Discord");
            _settings = options.Value.Discord;
            _logger = logger;
        }

        public async Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                username = _settings.Username,
                avatar_url = _settings.AvatarUrl,
                embeds = new[]
                {
                    new
                    {
                        title       = record.Title,
                        description = record.Message,
                        color       = LevelColours.GetValueOrDefault(record.Level, 0x95A5A6),
                        fields      = new[]
                        {
                            new { name = "Level",  value = record.Level.ToString(),  inline = true },
                            new { name = "Source", value = record.Source,            inline = true },
                            new { name = "Time",   value = record.Timestamp.ToString("u"), inline = false }
                        },
                        footer = new { text = $"ID: {record.Id}" }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.WebhookUrl, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Discord webhook call failed. Status={Status} Body={Body}", response.StatusCode, body);

                throw new HttpRequestException($"Discord webhook failed with status {response.StatusCode}");
            }
            else
            {
                _logger.LogInformation("Notification forwarded to Discord. Id={Id} Level={Level}", record.Id, record.Level);
            }
        }
    }
}
