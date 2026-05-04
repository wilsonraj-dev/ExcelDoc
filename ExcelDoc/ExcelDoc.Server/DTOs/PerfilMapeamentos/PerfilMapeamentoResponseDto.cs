using System.Text.Json.Serialization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoResponseDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("fk_IdDocumento")]
        public int FK_IdDocumento { get; set; }

        [JsonPropertyName("fk_IdEmpresa")]
        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public DateTime DataCriacao { get; set; }

        public List<PerfilMapeamentoItemResponseDto> Itens { get; set; } = new();
    }
}
