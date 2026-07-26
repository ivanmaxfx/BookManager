namespace MyWebApiProject.Models
{
    /// <summary>
    /// Статус бронирования.
    /// </summary>
    public enum BookingStatus
    {
        /// <summary>
        /// Бронирование ожидает обработки.
        /// </summary>
        Pending,

        /// <summary>
        /// Бронирование подтверждено.
        /// </summary>
        Confirmed,

        /// <summary>
        /// Бронирование отклонено.
        /// </summary>
        Rejected
    }
}
