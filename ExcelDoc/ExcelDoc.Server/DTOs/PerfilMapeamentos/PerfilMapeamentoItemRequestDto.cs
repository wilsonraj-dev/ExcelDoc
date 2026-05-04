using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoItemRequestDto
    {
        [Required(ErrorMessage = "Coleção é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "Coleção inválida.")]
        public int FK_IdColecao { get; set; }

        [Required(ErrorMessage = "Mapeamento é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mapeamento inválido.")]
        public int FK_IdMapeamento { get; set; }
    }
}
