namespace BookManager.Application.Dtos
{
    /// <summary>
    /// Параметры фильтрации и пагинации мероприятий.
    /// </summary>
    public class EventQueryParameters
    {
        /// <summary>
        /// Поиск по названию.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// События, начинающиеся не раньше указанной даты.
        /// </summary>
        public DateTime? From { get; set; }

        /// <summary>
        /// События, заканчивающиеся не позже указанной даты.
        /// </summary>
        public DateTime? To { get; set; }

        /// <summary>
        /// Номер страницы.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Размер страницы.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
