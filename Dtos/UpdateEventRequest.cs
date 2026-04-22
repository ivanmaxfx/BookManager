using System.ComponentModel.DataAnnotations;

namespace MyWebApiProject.Dtos
{
    public class UpdateEventRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }
    }
}