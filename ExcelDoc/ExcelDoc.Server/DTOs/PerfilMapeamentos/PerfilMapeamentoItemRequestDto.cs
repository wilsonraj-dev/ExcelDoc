using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoItemRequestDto
    {
        [Required(ErrorMessage = "Coleção é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "Coleção inválida.")]
        [JsonPropertyName("fk_IdColecao")]
        public int FK_IdColecao { get; set; }

        [Required(ErrorMessage = "Mapeamento é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mapeamento inválido.")]
        [JsonPropertyName("fk_IdMapeamento")]
        public int FK_IdMapeamento { get; set; }

        [JsonPropertyName("fk_IdColecaoPai")]
        public int? FK_IdColecaoPai { get; set; }
    }
}
