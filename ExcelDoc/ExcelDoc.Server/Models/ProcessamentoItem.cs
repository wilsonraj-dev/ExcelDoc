using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class ProcessamentoItem
    {
        public int Id { get; set; }

        public int FK_IdProcessamento { get; set; }

        public int? IdExcel { get; set; }

        [MaxLength(128)]
        public string? IdDocumentoUnico { get; set; }

        public int LinhaExcel { get; set; }

        [Required]
        public string JsonEnviado { get; set; } = string.Empty;

        public string? JsonRetorno { get; set; }

        [MaxLength(1000)]
        public string? Mensagem { get; set; }

        [MaxLength(4000)]
        public string? Erro { get; set; }

        public StatusProcessamentoItem Status { get; set; }

        public DateTime? DataExecucao { get; set; }

        public DateTime? DataFinalizacao { get; set; }

        public Processamento Processamento { get; set; } = null!;
    }
}
