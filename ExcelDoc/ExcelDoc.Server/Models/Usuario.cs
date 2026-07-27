using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeUsuario { get; set; } = string.Empty;

        public TipoUsuario TipoUsuario { get; set; }

        public bool Ativo { get; set; } = true;

        [Required]
        [MaxLength(5)]
        public string Idioma { get; set; } = "pt";
    }
}
