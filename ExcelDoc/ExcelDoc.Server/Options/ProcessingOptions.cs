namespace ExcelDoc.Server.Options
{
    public class ProcessingOptions
    {
        public const string SectionName = "Processing";

        public int MaxRetries { get; set; } = 3;

        public int QueueCapacity { get; set; } = 100;

        public int SapRequestsPerSecond { get; set; } = 3;

        public int MaxPageSize { get; set; } = 100;
    }
}
