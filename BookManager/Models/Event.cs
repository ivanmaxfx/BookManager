namespace MyWebApiProject.Models
{
    /// <summary>
    /// Представляет мероприятие.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Уникальный идентификатор мероприятия.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название мероприятия.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Описание мероприятия.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала мероприятия.
        /// </summary>
        public DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания мероприятия.
        /// </summary>
        public DateTime EndAt { get; set; }
    }
}