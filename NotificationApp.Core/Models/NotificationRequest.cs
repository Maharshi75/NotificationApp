using System.ComponentModel.DataAnnotations;

namespace NotificationApp.Core.Models
{
    public class NotificationRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
        [MinLength(1, ErrorMessage = "Title cannot be empty.")]
        public string Title { get; init; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Message must not exceed 2000 characters.")]
        public string Message { get; init; } = string.Empty;
        
        [Required(ErrorMessage = "Level is required.")]
        public string Level { get; init; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Source must not exceed 100 characters.")]
        public string? Source { get; init; }
        
        public DateTime Timestamp { get; init; }
    }
}
