namespace ExcelDoc.Server.DTOs.Auth
{
    public class RegisterUserResponseDto
    {
        public int UsuarioId { get; set; }

        public string NomeUsuario { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
