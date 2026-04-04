using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class ProcessamentoResponseDto
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public int EmpresaId { get; set; }

        public int DocumentoId { get; set; }

        public string NomeArquivo { get; set; } = string.Empty;

        public DateTime DataExecucao { get; set; }

        public StatusProcessamento Status { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalSucesso { get; set; }

        public int TotalErro { get; set; }

        public string HashArquivo { get; set; } = string.Empty;
    }
}
