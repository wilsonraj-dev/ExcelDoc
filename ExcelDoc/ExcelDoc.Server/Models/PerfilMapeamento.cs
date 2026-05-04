using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class PerfilMapeamento
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        public int FK_IdDocumento { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public DateTime DataCriacao { get; set; }

        public Documento Documento { get; set; } = null!;

        public Empresa? Empresa { get; set; }

        public ICollection<PerfilMapeamentoItem> Itens { get; set; } = new List<PerfilMapeamentoItem>();
    }
}
