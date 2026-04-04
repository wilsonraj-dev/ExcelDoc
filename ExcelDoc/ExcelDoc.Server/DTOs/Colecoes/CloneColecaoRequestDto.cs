using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Colecoes
{
    public class CloneColecaoRequestDto
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ColecaoPadraoId { get; set; }

        [Required]
        [MaxLength(150)]
        public string NomeColecao { get; set; } = string.Empty;
    }
}
