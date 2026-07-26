namespace MyWebApiProject.Dtos
{
    /// <summary>
    /// Результат пагинации.
    /// </summary>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Общее количество элементов после фильтрации.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Номер текущей страницы.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Количество элементов на текущей странице.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Элементы текущей страницы.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    }
}