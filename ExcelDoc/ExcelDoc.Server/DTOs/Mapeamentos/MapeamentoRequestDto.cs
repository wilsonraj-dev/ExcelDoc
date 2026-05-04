using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoRequestDto
    {
        [Required(ErrorMessage = "Nome do mapeamento é obrigatório.")]
        [MaxLength(150, ErrorMessage = "Nome do mapeamento deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Coleção é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "Coleção inválida.")]
        public int FK_IdColecao { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }
    }
}
