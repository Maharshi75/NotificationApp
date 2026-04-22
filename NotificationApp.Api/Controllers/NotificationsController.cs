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
        ///   200 OK           — logged only (below forward threshold)
        ///   202 Accepted     — forwarded to external sender
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
                    message = "Notification received and logged. Level is below the forward threshold."
                }),

                ProcessResult.Forwarded => Accepted(new
                {
                    status = "forwarded",
                    message = "Notification received and forwarded successfully."
                }),

                ProcessResult.RateLimitReached => StatusCode(429, new
                {
                    status = "rate_limit_reached",
                    message = "Maximum of 10 notifications per minute reached. Please try again later."
                }),

                ProcessResult.InvalidLevel => BadRequest(new
                {
                    status = "invalid_level",
                    message = $"'{request.Level}' is not a recognised notification level. Valid values: Debug, Info, Warning, Error, Critical."
                }),

                ProcessResult.ForwardingFailed => StatusCode(502, new
                {
                    status = "forwarding_failed",
                    message = "Notification was received but could not be forwarded to Discord. Check your webhook URL."
                }),

                _ => StatusCode(500, new { status = "error", message = "Unexpected processing result." })
            };
        }
    }
}
