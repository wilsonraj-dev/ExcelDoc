namespace ExcelDoc.Server.DTOs.Usuarios
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }

        public string NomeUsuario { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string TipoUsuario { get; set; } = string.Empty;

        public bool Ativo { get; set; }

        public int? EmpresaId { get; set; }

        public string? NomeEmpresa { get; set; }

        public string Idioma { get; set; } = "pt";
    }
}
