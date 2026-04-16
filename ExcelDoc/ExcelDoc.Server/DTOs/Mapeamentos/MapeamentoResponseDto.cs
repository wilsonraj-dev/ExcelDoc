using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoResponseDto
    {
        public int Id { get; set; }

        public string NomeCampo { get; set; } = string.Empty;

        public string DescricaoCampo { get; set; } = string.Empty;

        public int IndiceColuna { get; set; }

        public TipoCampo TipoCampo { get; set; }

        public string? Formato { get; set; }

        public int FK_IdColecao { get; set; }
    }
}
