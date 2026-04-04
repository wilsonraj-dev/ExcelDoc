namespace ExcelDoc.Server.DTOs.Documentos
{
    public class DocumentoResponseDto
    {
        public int Id { get; set; }

        public string NomeDocumento { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;
    }
}
