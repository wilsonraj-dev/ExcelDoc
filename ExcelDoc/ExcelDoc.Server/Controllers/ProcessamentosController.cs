using ExcelDoc.Server.DTOs.Processamentos;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessamentosController : ControllerBase
    {
        private readonly IProcessamentoService _processamentoService;

        public ProcessamentosController(IProcessamentoService processamentoService)
        {
            _processamentoService = processamentoService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Upload([FromForm] UploadProcessamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _processamentoService.CriarEEnfileirarAsync(request, cancellationToken);
                return AcceptedAtAction(nameof(GetById), new { processamentoId = result.Id, usuarioExecutorId = request.UsuarioExecutorId }, result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("{processamentoId:int}")]
        public async Task<IActionResult> GetById(int processamentoId, [FromQuery] int usuarioExecutorId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _processamentoService.GetByIdAsync(processamentoId, usuarioExecutorId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ProcessamentoQueryDto query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _processamentoService.GetPagedAsync(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("{processamentoId:int}/itens")]
        public async Task<IActionResult> GetItens(int processamentoId, [FromQuery] ProcessamentoItensQueryDto query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _processamentoService.GetItemsPagedAsync(processamentoId, query, cancellationToken);
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
                FormatException => BadRequest(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status400BadRequest }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status500InternalServerError })
            };
        }
    }
}
