using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Processamento
    {
        public int Id { get; set; }

        public int FK_IdUsuario { get; set; }

        public int FK_IdEmpresa { get; set; }

        public int FK_IdDocumento { get; set; }

        [Required]
        [MaxLength(255)]
        public string NomeArquivo { get; set; } = string.Empty;

        public DateTime DataExecucao { get; set; }

        public StatusProcessamento Status { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalSucesso { get; set; }

        public int TotalErro { get; set; }

        [Required]
        [MaxLength(200)]
        public string HashArquivo { get; set; } = string.Empty;

        public Documento Documento { get; set; } = null!;

        public Empresa Empresa { get; set; } = null!;

        public ICollection<ProcessamentoItem> Itens { get; set; } = new List<ProcessamentoItem>();

        public Usuario Usuario { get; set; } = null!;
    }
}
