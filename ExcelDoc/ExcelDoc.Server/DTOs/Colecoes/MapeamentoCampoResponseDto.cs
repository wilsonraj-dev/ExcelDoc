using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class MapeamentoCampoResponseDto
    {
        public int Id { get; set; }

        public int IndiceColuna { get; set; }

        public string NomeCampo { get; set; } = string.Empty;

        public string DescricaoCampo { get; set; } = string.Empty;

        public TipoCampo TipoCampo { get; set; }

        public string? Formato { get; set; }
    }
}
