using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.Models
{
    public class Colecao
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeColecao { get; set; } = string.Empty;

        public TipoColecao TipoColecao { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public Empresa? Empresa { get; set; }

        public ICollection<DocumentoColecao> DocumentoColecoes { get; set; } = new List<DocumentoColecao>();

        public ICollection<MapeamentoCampo> MapeamentoCampos { get; set; } = new List<MapeamentoCampo>();
    }
}
