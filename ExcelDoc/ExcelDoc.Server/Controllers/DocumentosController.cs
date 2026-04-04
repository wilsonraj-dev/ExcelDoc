using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AuthRoles.All)]
    public class DocumentosController : ControllerBase
    {
        private readonly IDocumentoService _documentoService;

        public DocumentosController(IDocumentoService documentoService)
        {
            _documentoService = documentoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await _documentoService.GetAllAsync(cancellationToken);
            return Ok(result);
        }
    }
}
