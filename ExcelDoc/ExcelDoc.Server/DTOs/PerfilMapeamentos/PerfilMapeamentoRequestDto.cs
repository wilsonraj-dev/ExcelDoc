using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoRequestDto
    {
        [Required(ErrorMessage = "Nome do perfil é obrigatório.")]
        [MaxLength(150, ErrorMessage = "Nome do perfil deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Documento é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Documento inválido.")]
        public int FK_IdDocumento { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public List<PerfilMapeamentoItemRequestDto> Itens { get; set; } = new();
    }
}
