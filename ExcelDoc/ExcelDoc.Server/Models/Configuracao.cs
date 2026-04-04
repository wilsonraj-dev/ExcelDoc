using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Configuracao
    {
        public int Id { get; set; }

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

        public int FK_IdEmpresa { get; set; }

        public Empresa Empresa { get; set; } = null!;
    }
}
