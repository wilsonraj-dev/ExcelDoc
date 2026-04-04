namespace ExcelDoc.Server.DTOs.Configuracoes
{
    public class ConfiguracaoResponseDto
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }

        public string LinkServiceLayer { get; set; } = string.Empty;

        public string Database { get; set; } = string.Empty;

        public string UsuarioBanco { get; set; } = string.Empty;

        public string SenhaBanco { get; set; } = string.Empty;

        public string UsuarioSAP { get; set; } = string.Empty;

        public string SenhaSAP { get; set; } = string.Empty;
    }
}
