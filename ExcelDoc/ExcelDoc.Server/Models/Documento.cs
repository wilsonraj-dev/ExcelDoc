using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Documento
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeDocumento { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Endpoint { get; set; } = string.Empty;

        public ICollection<DocumentoColecao> DocumentoColecoes { get; set; } = new List<DocumentoColecao>();

        public ICollection<Processamento> Processamentos { get; set; } = new List<Processamento>();
    }
}
