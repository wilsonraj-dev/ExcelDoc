using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Configuracoes
{
    public class ConfiguracaoRequestDto
    {
        [Required]
        public int UsuarioExecutorId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [MaxLength(500)]
        public string LinkServiceLayer { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Database { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string UsuarioBanco { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string SenhaBanco { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string UsuarioSAP { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string SenhaSAP { get; set; } = string.Empty;
    }
}
