using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/mapeamentos-campos")]
    [Authorize(Roles = AuthRoles.All)]
    public class MapeamentosCamposController : ControllerBase
    {
        private readonly IMapeamentoCampoService _mapeamentoCampoService;

        public MapeamentosCamposController(IMapeamentoCampoService mapeamentoCampoService)
        {
            _mapeamentoCampoService = mapeamentoCampoService;
        }

        [HttpGet("{mapeamentoId:int}")]
        public async Task<IActionResult> GetByMapeamento(int mapeamentoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoCampoService.GetByMapeamentoAsync(mapeamentoId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MapeamentoCampoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoCampoService.CriarAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] MapeamentoCampoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoCampoService.AtualizarAsync(id, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _mapeamentoCampoService.ExcluirAsync(id, cancellationToken);
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
