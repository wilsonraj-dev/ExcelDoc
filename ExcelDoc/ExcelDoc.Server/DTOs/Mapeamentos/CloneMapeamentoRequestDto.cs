using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Localization;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class CloneMapeamentoRequestDto
    {
        [Required(ErrorMessage = MessageKeys.CloneNameRequired)]
        [MaxLength(150, ErrorMessage = MessageKeys.CloneNameMaxLength)]
        public string Nome { get; set; } = string.Empty;
    }
}
