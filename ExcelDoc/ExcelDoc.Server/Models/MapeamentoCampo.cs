using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class MapeamentoCampo
    {
        public int Id { get; set; }

        public int IndiceColuna { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeCampo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string DescricaoCampo { get; set; } = string.Empty;

        public TipoCampo TipoCampo { get; set; }

        [MaxLength(50)]
        public string? Formato { get; set; }

        public int FK_IdColecao { get; set; }

        public Colecao Colecao { get; set; } = null!;
    }
}
