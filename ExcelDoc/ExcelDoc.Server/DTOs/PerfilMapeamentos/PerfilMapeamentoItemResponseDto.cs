using System.Text.Json.Serialization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoItemResponseDto
    {
        public int Id { get; set; }

        [JsonPropertyName("fk_IdColecao")]
        public int FK_IdColecao { get; set; }

        public string NomeColecao { get; set; } = string.Empty;

        [JsonPropertyName("fk_IdMapeamento")]
        public int FK_IdMapeamento { get; set; }

        public string NomeMapeamento { get; set; } = string.Empty;

        [JsonPropertyName("fk_IdPerfilMapeamentoItemPai")]
        public int? FK_IdPerfilMapeamentoItemPai { get; set; }

        [JsonPropertyName("fk_IdColecaoPai")]
        public int? FK_IdColecaoPai { get; set; }

        public string? NomeColecaoPai { get; set; }
    }
}
