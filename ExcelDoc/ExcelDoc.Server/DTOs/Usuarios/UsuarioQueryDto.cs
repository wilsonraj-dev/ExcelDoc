namespace ExcelDoc.Server.DTOs.Usuarios
{
    public class UsuarioQueryDto
    {
        public string? Termo { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
