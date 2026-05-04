namespace ExcelDoc.Server.DTOs.PerfilMapeamentos
{
    public class PerfilMapeamentoItemResponseDto
    {
        public int Id { get; set; }

        public int FK_IdColecao { get; set; }

        public string NomeColecao { get; set; } = string.Empty;

        public int FK_IdMapeamento { get; set; }

        public string NomeMapeamento { get; set; } = string.Empty;
    }
}
