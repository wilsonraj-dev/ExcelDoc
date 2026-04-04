using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ExcelDoc.Server.DTOs.Processamentos
{
    public class UploadProcessamentoRequestDto
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int DocumentoId { get; set; }

        [Required]
        public IFormFile Arquivo { get; set; } = null!;
    }
}
