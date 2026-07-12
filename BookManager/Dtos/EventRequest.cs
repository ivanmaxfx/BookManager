using System.ComponentModel.DataAnnotations;

namespace MyWebApiProject.Dtos
{
    /// <summary>
    /// DTO для создания и обновления мероприятия.
    /// </summary>
    public class EventRequest : IValidatableObject
    {
        /// <summary>
        /// Название мероприятия.
        /// </summary>
        [Required]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Описание мероприятия.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия.
        /// </summary>
        [Required]
        public DateTime? StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия.
        /// </summary>
        [Required]
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// Выполняет дополнительную валидацию модели.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                yield return new ValidationResult(
                    "Title is required.",
                    new[] { nameof(Title) });
            }

            if (StartAt.HasValue && EndAt.HasValue && EndAt <= StartAt)
            {
                yield return new ValidationResult(
                    "EndAt must be later than StartAt.",
                    new[] { nameof(EndAt) });
            }
        }
    }
}