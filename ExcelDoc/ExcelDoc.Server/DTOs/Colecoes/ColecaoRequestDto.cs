using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class ColecaoRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string NomeColecao { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descricao { get; set; }

        [Required]
        public TipoColecao TipoColecao { get; set; }

        public bool IsPadrao { get; set; }

        public IReadOnlyCollection<int> DocumentoIds { get; set; } = Array.Empty<int>();
    }
}
