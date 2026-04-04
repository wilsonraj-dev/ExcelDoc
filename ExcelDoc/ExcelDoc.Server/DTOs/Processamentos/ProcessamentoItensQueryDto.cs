namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class ProcessamentoItensQueryDto
    {
        public int UsuarioExecutorId { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
