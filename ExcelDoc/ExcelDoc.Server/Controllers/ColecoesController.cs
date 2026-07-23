using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AuthRoles.All)]
    public class ColecoesController : ControllerBase
    {
        private readonly IColecaoService _colecaoService;

        public ColecoesController(IColecaoService colecaoService)
        {
            _colecaoService = colecaoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? empresaId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _colecaoService.GetByEmpresaIdAsync(empresaId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("{colecaoId:int}")]
        public async Task<IActionResult> GetById(int colecaoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _colecaoService.GetByIdAsync(colecaoId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        [Authorize(Roles = AuthRoles.Administrador)]
        public async Task<IActionResult> Post([FromBody] ColecaoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _colecaoService.CriarAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{colecaoId:int}")]
        [Authorize(Roles = AuthRoles.Administrador)]
        public async Task<IActionResult> Put(int colecaoId, [FromBody] ColecaoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _colecaoService.AtualizarAsync(colecaoId, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpDelete("{colecaoId:int}")]
        [Authorize(Roles = AuthRoles.Administrador)]
        public async Task<IActionResult> Delete(int colecaoId, CancellationToken cancellationToken)
        {
            try
            {
                await _colecaoService.ExcluirAsync(colecaoId, cancellationToken);
                return NoContent();
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
