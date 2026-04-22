using Microsoft.AspNetCore.Mvc;
using NotificationApp.Core.Models;
using NotificationApp.Core.Services;

namespace NotificationApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationProcessor _processor;

        public NotificationsController(NotificationProcessor processor)
        {
            _processor = processor;
        }

        /// <summary>
        /// Receives a notification payload and processes it.
        /// POST /api/notifications
        ///
        /// Responses:
        ///   200 OK           — logged only (below sent threshold)
        ///   202 Accepted     — sent to external sender
        ///   400 Bad Request  — invalid or unrecognised level
        ///   429 Too Many     — rate limit reached, try again later
        ///   502 Bad Geteway  - webhook url is wrong
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NotificationRequest request, CancellationToken cancellationToken)
        {
            var result = await _processor.ProcessAsync(request, cancellationToken);

            return result switch
            {
                ProcessResult.LoggedOnly => Ok(new
                {
                    status = "logged",
                    message = "Notification received and logged. Level is below Warning threshold."
                }),

                ProcessResult.Sent => Accepted(new
                {
                    status = "sent",
                    message = "Notification received and sent successfully."
                }),

                ProcessResult.RateLimitReached => StatusCode(429, new
                {
                    status = "rate_limit_reached",
                    message = "Maximum of 10 notifications per minute reached. Please try again later."
                }),

                ProcessResult.InvalidLevel => BadRequest(new
                {
                    status = "invalid_level",
                    message = $"'{request.Level}' is not a recognised notification level. Valid values: Info, Warning, Error, Critical."
                }),

                ProcessResult.SendFailed => StatusCode(502, new
                {
                    status = "send_failed",
                    message = "Notification was received but could not be sent to Discord. Check your webhook URL."
                }),

                _ => StatusCode(500, new { status = "error", message = "Unexpected processing result." })
            };
        }
    }
}
