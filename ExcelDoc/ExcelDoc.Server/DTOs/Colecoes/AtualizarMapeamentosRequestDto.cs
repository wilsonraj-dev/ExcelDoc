using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class AtualizarMapeamentosRequestDto
    {
        [Required]
        public int UsuarioExecutorId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public IReadOnlyCollection<MapeamentoCampoRequestDto> Campos { get; set; } = Array.Empty<MapeamentoCampoRequestDto>();
    }
}
