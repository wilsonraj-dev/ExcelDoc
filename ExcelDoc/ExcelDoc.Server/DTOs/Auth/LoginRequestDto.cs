using System.ComponentModel.DataAnnotations;

namespace ExcelDoc.Server.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Database { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [MaxLength(254)]
        public string Senha { get; set; } = string.Empty;
    }
}
