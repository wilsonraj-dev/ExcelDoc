using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string SenhaHash { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        public TipoUsuario TipoUsuario { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool Ativo { get; set; } = true;

        public Empresa? Empresa { get; set; }

        public ICollection<Processamento> Processamentos { get; set; } = new List<Processamento>();
    }
}
