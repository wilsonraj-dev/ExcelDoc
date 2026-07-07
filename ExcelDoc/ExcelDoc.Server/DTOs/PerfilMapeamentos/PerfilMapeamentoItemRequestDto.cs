using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExcelDoc.Server.Localization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoItemRequestDto
    {
        [Required(ErrorMessage = MessageKeys.CollectionRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.CollectionInvalid)]
        [JsonPropertyName("fk_IdColecao")]
        public int FK_IdColecao { get; set; }

        [Required(ErrorMessage = MessageKeys.MappingRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.MappingInvalid)]
        [JsonPropertyName("fk_IdMapeamento")]
        public int FK_IdMapeamento { get; set; }

        [JsonPropertyName("fk_IdColecaoPai")]
        public int? FK_IdColecaoPai { get; set; }
    }
}
