namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class ProcessamentoItensQueryDto
    {
        public Models.StatusProcessamentoItem? Status { get; set; }

        public bool ApenasComErro { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
