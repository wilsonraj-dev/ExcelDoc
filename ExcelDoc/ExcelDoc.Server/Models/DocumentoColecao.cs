namespace ExcelDoc.Server.Models
{
    public class DocumentoColecao
    {
        public int Id { get; set; }

        public int FK_IdDocumento { get; set; }

        public int FK_IdColecao { get; set; }

        public Colecao Colecao { get; set; } = null!;

        public Documento Documento { get; set; } = null!;
    }
}
