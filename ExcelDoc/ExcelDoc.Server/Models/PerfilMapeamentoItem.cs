namespace ExcelDoc.Server.Models
{
    public class PerfilMapeamentoItem
    {
        public int Id { get; set; }

        public int FK_IdPerfilMapeamento { get; set; }

        public int FK_IdColecao { get; set; }

        public int FK_IdMapeamento { get; set; }

        public PerfilMapeamento PerfilMapeamento { get; set; } = null!;

        public Colecao Colecao { get; set; } = null!;

        public Mapeamento Mapeamento { get; set; } = null!;
    }
}
