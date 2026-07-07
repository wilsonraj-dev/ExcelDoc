using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExcelDoc.Server.Localization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoRequestDto
    {
        [Required(ErrorMessage = MessageKeys.ProfileNameRequired)]
        [MaxLength(150, ErrorMessage = MessageKeys.ProfileNameMaxLength)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = MessageKeys.DocumentRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.DocumentInvalid)]
        [JsonPropertyName("fk_IdDocumento")]
        public int FK_IdDocumento { get; set; }

        [JsonPropertyName("fk_IdEmpresa")]
        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public List<PerfilMapeamentoItemRequestDto> Itens { get; set; } = new();
    }
}
