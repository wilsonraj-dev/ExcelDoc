using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class ColecaoResponseDto
    {
        public int Id { get; set; }

        public string NomeColecao { get; set; } = string.Empty;

        public TipoColecao TipoColecao { get; set; }

        public int? EmpresaId { get; set; }

        public bool PadraoSistema => !EmpresaId.HasValue;

        public IReadOnlyCollection<MapeamentoCampoResponseDto> Campos { get; set; } = Array.Empty<MapeamentoCampoResponseDto>();
    }
}
