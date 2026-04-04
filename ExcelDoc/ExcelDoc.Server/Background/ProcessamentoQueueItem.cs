namespace ExcelDoc.Server.Background
{
    public class ProcessamentoQueueItem
    {
        public int ProcessamentoId { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public int Attempt { get; set; }
    }
}
