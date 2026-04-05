using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(200)]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
