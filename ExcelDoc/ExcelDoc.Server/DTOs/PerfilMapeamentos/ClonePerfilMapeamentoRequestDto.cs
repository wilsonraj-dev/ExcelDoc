using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class ClonePerfilMapeamentoRequestDto
    {
        [Required(ErrorMessage = "Nome do clone é obrigatório.")]
        [MaxLength(150, ErrorMessage = "Nome do clone deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }
}