using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Empresa
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string NomeEmpresa { get; set; } = string.Empty;

        public Configuracao? Configuracao { get; set; }

        public ICollection<Colecao> Colecoes { get; set; } = new List<Colecao>();

        public ICollection<Processamento> Processamentos { get; set; } = new List<Processamento>();

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
