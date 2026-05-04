namespace ExcelDoc.Server.DTOs.Mapeamentos
{
    public class MapeamentoResumoResponseDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int FK_IdColecao { get; set; }

        public int? FK_IdEmpresa { get; set; }

        public bool IsPadrao { get; set; }

        public int QuantidadeCampos { get; set; }
    }
}
