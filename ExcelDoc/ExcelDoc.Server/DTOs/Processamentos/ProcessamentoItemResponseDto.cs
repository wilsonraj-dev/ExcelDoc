using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class ProcessamentoItemResponseDto
    {
        public int Id { get; set; }

        public int? IdExcel { get; set; }

        public string? IdDocumentoUnico { get; set; }

        public int LinhaExcel { get; set; }

        public string JsonEnviado { get; set; } = string.Empty;

        public string? JsonRetorno { get; set; }

        public string? Mensagem { get; set; }

        public string? Erro { get; set; }

        public StatusProcessamentoItem Status { get; set; }

        public DateTime? DataExecucao { get; set; }

        public DateTime? DataFinalizacao { get; set; }
    }
}
