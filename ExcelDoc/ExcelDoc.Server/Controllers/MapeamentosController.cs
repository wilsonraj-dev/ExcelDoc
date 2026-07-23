using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AuthRoles.All)]
    public class MapeamentosController : ControllerBase
    {
        private readonly IExcelReaderService _excelReaderService;
        private readonly IMapeamentoService _mapeamentoService;
        private readonly IMessageService _messageService;

        public MapeamentosController(
            IMapeamentoService mapeamentoService,
            IExcelReaderService excelReaderService,
            IMessageService messageService)
        {
            _mapeamentoService = mapeamentoService;
            _excelReaderService = excelReaderService;
            _messageService = messageService;
        }

        [HttpPost("preview-excel")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> PreviewExcel(
            [FromForm] IFormFile? arquivo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (arquivo is null || arquivo.Length == 0)
                {
                    throw new FormatException(_messageService.Get(MessageKeys.FileRequired));
                }

                if (!string.Equals(Path.GetExtension(arquivo.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException(_messageService.Get(MessageKeys.OnlyExcelFilesAccepted));
                }

                await using var stream = arquivo.OpenReadStream();
                var columns = await _excelReaderService.ReadFirstRowAsync(stream, cancellationToken);
                return Ok(new ExcelPreviewResponseDto { Colunas = columns });
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpGet("colecao/{colecaoId:int}")]
        public async Task<IActionResult> GetByColecao(int colecaoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoService.GetByColecaoAsync(colecaoId, cancellationToken);
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
                var result = await _mapeamentoService.GetByIdAsync(id, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MapeamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoService.CriarAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPost("{id:int}/clone")]
        public async Task<IActionResult> Clone(int id, [FromBody] CloneMapeamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoService.ClonarAsync(id, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ToActionResult(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] MapeamentoRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mapeamentoService.AtualizarAsync(id, request, cancellationToken);
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
                await _mapeamentoService.ExcluirAsync(id, cancellationToken);
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
                FormatException => BadRequest(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status400BadRequest }),
                InvalidOperationException => Conflict(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status409Conflict }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status500InternalServerError })
            };
        }
    }
}
