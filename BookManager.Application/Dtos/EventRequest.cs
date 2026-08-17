using System.ComponentModel.DataAnnotations;

namespace BookManager.Application.Dtos
{
    public class EventRequest : IValidatableObject
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime? StartAt { get; set; }

        [Required]
        public DateTime? EndAt { get; set; }

        [Required]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "TotalSeats must be greater than 0.")]
        public int? TotalSeats { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                yield return new ValidationResult(
                    "Title is required.",
                    new[] { nameof(Title) });
            }

            if (StartAt.HasValue &&
                EndAt.HasValue &&
                EndAt <= StartAt)
            {
                yield return new ValidationResult(
                    "EndAt must be later than StartAt.",
                    new[] { nameof(EndAt) });
            }
        }
    }
}
