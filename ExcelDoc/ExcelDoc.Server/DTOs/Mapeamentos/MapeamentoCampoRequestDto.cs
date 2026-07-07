using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoCampoRequestDto
    {
        [Required(ErrorMessage = MessageKeys.FieldNameRequired)]
        [MaxLength(150, ErrorMessage = MessageKeys.FieldNameMaxLength)]
        public string NomeCampo { get; set; } = string.Empty;

        [Required(ErrorMessage = MessageKeys.ColumnIndexRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.ColumnIndexPositive)]
        public int IndiceColuna { get; set; }

        [Required(ErrorMessage = MessageKeys.FieldTypeRequired)]
        public TipoCampo TipoCampo { get; set; }

        [MaxLength(50, ErrorMessage = MessageKeys.FormatMaxLength)]
        public string? Formato { get; set; }

        [Required(ErrorMessage = MessageKeys.MappingRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.MappingInvalid)]
        public int FK_IdMapeamento { get; set; }
    }
}
