using System.ComponentModel.DataAnnotations;
using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoCampoRequestDto
    {
        [Required(ErrorMessage = "Nome do campo é obrigatório.")]
        [MaxLength(150, ErrorMessage = "Nome do campo deve ter no máximo 150 caracteres.")]
        public string NomeCampo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Índice da coluna é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Índice da coluna deve ser um número positivo.")]
        public int IndiceColuna { get; set; }

        [Required(ErrorMessage = "Tipo do campo é obrigatório.")]
        public TipoCampo TipoCampo { get; set; }

        [MaxLength(50, ErrorMessage = "Formato deve ter no máximo 50 caracteres.")]
        public string? Formato { get; set; }

        [Required(ErrorMessage = "Mapeamento é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mapeamento inválido.")]
        public int FK_IdMapeamento { get; set; }
    }
}
