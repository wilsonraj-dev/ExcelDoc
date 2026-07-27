namespace ExcelDoc.Server.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public string NomeUsuario { get; set; } = string.Empty;

        public string TipoUsuario { get; set; } = string.Empty;

        public string Database { get; set; } = string.Empty;

        public string Idioma { get; set; } = "pt";
    }
}
