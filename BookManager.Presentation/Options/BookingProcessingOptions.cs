namespace MyWebApiProject.Options
{
    /// <summary>
    /// Настройки фоновой обработки бронирований.
    /// </summary>
    public class BookingProcessingOptions
    {
        public int PollIntervalMilliseconds { get; set; } = 1000;

        public int ProcessingDelayMilliseconds { get; set; } = 2000;
    }
}
