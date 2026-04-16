using ExcelDoc.Server.Models;
using ExcelDoc.Server.DTOs.Documentos;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class ColecaoResponseDto
    {
        public int Id { get; set; }

        public string NomeColecao { get; set; } = string.Empty;

        public TipoColecao TipoColecao { get; set; }

        public int? EmpresaId { get; set; }

        public bool PadraoSistema => !EmpresaId.HasValue;

        public IReadOnlyCollection<int> DocumentoIds { get; set; } = Array.Empty<int>();

        public IReadOnlyCollection<DocumentoResponseDto> Documentos { get; set; } = Array.Empty<DocumentoResponseDto>();

        public IReadOnlyCollection<MapeamentoCampoResponseDto> Campos { get; set; } = Array.Empty<MapeamentoCampoResponseDto>();
    }
}
