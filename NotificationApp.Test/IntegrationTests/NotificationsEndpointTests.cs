using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationApp.Api.Controllers;
using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using NotificationApp.Core.Services;
using System.Net;
using System.Net.Http.Json;

namespace NotificationApp.Test.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<NotificationsController>
    {
        public IDiscordSender? DiscordSenderOverride { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                // Remove every IDiscordSender registration
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(IDiscordSender))
                    .ToList();
                foreach (var d in descriptors)
                    services.Remove(d);

                // Use override if provided, otherwise use mock
                if (DiscordSenderOverride != null)
                    services.AddSingleton(DiscordSenderOverride);
                else
                    services.AddSingleton<IDiscordSender, MockDiscordSender>();
            });
        }
    }

    public class NotificationsEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public NotificationsEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(Action<IServiceCollection>? overrides = null)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    overrides?.Invoke(services);
                });
            }).CreateClient();
        }

        [Theory]
        [InlineData("Info")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        public async Task Post_ValidLevel_Returns202Accepted(string level)
        {
            var mockSender = new Mock<IDiscordSender>();
            mockSender
                .Setup(s => s.SendAsync(It.IsAny<NotificationRecord>(), default))
                .Returns(Task.CompletedTask);

            _factory.DiscordSenderOverride = mockSender.Object;
            var client = _factory.CreateClient();

            var payload = new
            {
                title = "Test notification",
                message = "Test message",
                level = level,
                source = "TestService"
            };

            var response = await client.PostAsJsonAsync("/api/notifications", payload);

            _factory.DiscordSenderOverride = null;

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Fact]
        public async Task Post_InvalidLevel_Returns400BadRequest()
        {
            var client = CreateClient();

            var payload = new
            {
                title = "Test",
                message = "Test message",
                level = "SuperCritical"
            };

            var response = await client.PostAsJsonAsync("/api/notifications", payload);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_MissingTitle_Returns400BadRequest()
        {
            var client = CreateClient();

            var payload = new
            {
                message = "Missing title",
                level = "Warning"
            };

            var response = await client.PostAsJsonAsync("/api/notifications", payload);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_RateLimitExceeded_Returns429()
        {
            var mockRateLimiter = new Mock<IRateLimiter>();
            mockRateLimiter.Setup(r => r.TryConsume()).Returns(false);

            var client = CreateClient(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IRateLimiter));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(mockRateLimiter.Object);
            });

            var payload = new
            {
                title = "High memory",
                message = "Memory usage at 95%",
                level = "Warning"
            };

            var response = await client.PostAsJsonAsync("/api/notifications", payload);
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
    }
}