using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Auth
{
    public class RegisterUserRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(200)]
        public string Senha { get; set; } = string.Empty;
    }
}
