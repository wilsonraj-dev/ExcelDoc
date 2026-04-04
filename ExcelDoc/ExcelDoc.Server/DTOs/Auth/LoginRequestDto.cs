using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Senha { get; set; } = string.Empty;
    }
}
