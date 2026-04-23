using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Mapeamento
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        public int FK_IdColecao { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public DateTime DataCriacao { get; set; }

        public Colecao Colecao { get; set; } = null!;

        public Empresa? Empresa { get; set; }

        public ICollection<MapeamentoCampo> Campos { get; set; } = new List<MapeamentoCampo>();
    }
}