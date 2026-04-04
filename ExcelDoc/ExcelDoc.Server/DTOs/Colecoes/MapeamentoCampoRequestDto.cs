using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class MapeamentoCampoRequestDto
    {
        public int? Id { get; set; }

        [Required]
        public int IndiceColuna { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeCampo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string DescricaoCampo { get; set; } = string.Empty;

        [Required]
        public TipoCampo TipoCampo { get; set; }

        [MaxLength(50)]
        public string? Formato { get; set; }
    }
}
