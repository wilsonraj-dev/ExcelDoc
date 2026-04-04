using ExcelDoc.Server.DTOs.Configuracoes;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracoesController : ControllerBase
    {
        private readonly IConfiguracaoService _configuracaoService;

        public ConfiguracoesController(IConfiguracaoService configuracaoService)
        {
            _configuracaoService = configuracaoService;
        }

        [HttpGet("{empresaId:int}")]
        public async Task<IActionResult> GetByEmpresaId(int empresaId, [FromQuery] int usuarioExecutorId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _configuracaoService.GetByEmpresaIdAsync(empresaId, usuarioExecutorId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ConfiguracaoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _configuracaoService.UpsertAsync(request, cancellationToken);
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
