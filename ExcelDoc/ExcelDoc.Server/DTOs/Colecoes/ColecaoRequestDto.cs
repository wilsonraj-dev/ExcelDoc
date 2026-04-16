using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class ColecaoRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string NomeColecao { get; set; } = string.Empty;

        [Required]
        public TipoColecao TipoColecao { get; set; }

        [JsonPropertyName("fk_IdEmpresa")]
        public int? FK_IdEmpresa { get; set; }

        public IReadOnlyCollection<int> DocumentoIds { get; set; } = Array.Empty<int>();
    }
}
