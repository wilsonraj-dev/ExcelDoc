using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Localization;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class ClonePerfilMapeamentoRequestDto
    {
        [Required(ErrorMessage = MessageKeys.CloneNameRequired)]
        [MaxLength(150, ErrorMessage = MessageKeys.CloneNameMaxLength)]
        public string Nome { get; set; } = string.Empty;
    }
}
