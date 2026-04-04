using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class ProcessamentoQueryDto
    {
        public int EmpresaId { get; set; }

        public StatusProcessamento? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
