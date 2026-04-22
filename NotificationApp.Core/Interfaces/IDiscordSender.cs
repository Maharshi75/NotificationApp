using NotificationApp.Core.Models;

namespace NotificationApp.Core.Interfaces
{
    public interface IDiscordSender
    {
        /// Sends or simulates sending a notification record to the external interface.
        Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default);
    }
}
