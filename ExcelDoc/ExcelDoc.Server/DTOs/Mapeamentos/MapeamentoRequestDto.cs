using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Localization;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoRequestDto
    {
        [Required(ErrorMessage = MessageKeys.MappingNameRequired)]
        [MaxLength(150, ErrorMessage = MessageKeys.MappingNameMaxLength)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = MessageKeys.CollectionRequired)]
        [Range(1, int.MaxValue, ErrorMessage = MessageKeys.CollectionInvalid)]
        public int FK_IdColecao { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }
    }
}
