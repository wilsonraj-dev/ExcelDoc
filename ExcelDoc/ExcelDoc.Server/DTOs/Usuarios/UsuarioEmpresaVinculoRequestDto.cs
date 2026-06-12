using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Usuarios
{
    public class UsuarioEmpresaVinculoRequestDto
    {
        [Required]
        public int EmpresaId { get; set; }
    }
}
