using ExcelDoc.Server.DTOs.Usuarios;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AuthRoles.Administrador)]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] UsuarioQueryDto query, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _usuarioService.GetPagedAsync(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UsuarioCreateRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _usuarioService.CriarAsync(request, cancellationToken);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{usuarioId:int}/empresa")]
        public async Task<IActionResult> PutVinculoEmpresa(int usuarioId, [FromBody] UsuarioEmpresaVinculoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _usuarioService.VincularEmpresaAsync(usuarioId, request, cancellationToken);
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

        [HttpPut("idioma")]
        [Authorize]
        public async Task<IActionResult> PutIdioma([FromBody] DTOs.Usuarios.AtualizarIdiomaDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                await _usuarioService.AtualizarIdioma(userId, dto.Idioma, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }
    }
}
