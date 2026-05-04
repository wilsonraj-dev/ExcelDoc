using ExcelDoc.Server.DTOs.PerfilMapeamentos;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/perfil-mapeamento")]
    [Authorize(Roles = AuthRoles.All)]
    public class PerfilMapeamentosController : ControllerBase
    {
        private readonly IPerfilMapeamentoService _service;

        public PerfilMapeamentosController(IPerfilMapeamentoService service)
        {
            _service = service;
        }

        [HttpGet("documento/{documentoId:int}")]
        public async Task<IActionResult> GetByDocumento(int documentoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.GetByDocumentoAsync(documentoId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PerfilMapeamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CriarAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] PerfilMapeamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.AtualizarAsync(id, request, cancellationToken);
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
                await _service.ExcluirAsync(id, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost("{id:int}/clone")]
        public async Task<IActionResult> Clone(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.ClonarAsync(id, cancellationToken);
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
