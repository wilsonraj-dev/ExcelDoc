using ExcelDoc.Server.Security;
using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Services.Interfaces;
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
            try
            {
                var result = await _documentoService.GetAllAsync(cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("{documentoId:int}")]
        public async Task<IActionResult> GetById(int documentoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _documentoService.GetByIdAsync(documentoId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DocumentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _documentoService.CriarAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpDelete("{documentoId:int}")]
        public async Task<IActionResult> Delete(int documentoId, CancellationToken cancellationToken)
        {
            try
            {
                await _documentoService.ExcluirAsync(documentoId, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{documentoId:int}")]
        public async Task<IActionResult> Put(int documentoId, [FromBody] DocumentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _documentoService.AtualizarAsync(documentoId, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        private ObjectResult ToActionResult(Exception ex)
        {
            return ex switch
            {
                KeyNotFoundException => NotFound(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status404NotFound }),
                UnauthorizedAccessException => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status403Forbidden }),
                InvalidOperationException => Conflict(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status409Conflict }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status500InternalServerError })
            };
        }
    }
}
