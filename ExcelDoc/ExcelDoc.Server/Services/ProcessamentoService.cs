using System.Text.Json;
using ExcelDoc.Server.Background;
using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.DTOs;
using ExcelDoc.Server.DTOs.Processamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services
{
    public class ProcessamentoService : IProcessamentoService
    {
        private const string RequestPayloadKey = "RequestPayload";
        private const string ResponseBodyKey = "ResponseBody";
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IArquivoStorageService _arquivoStorageService;
        private readonly IHashArquivoService _hashArquivoService;
        private readonly IProcessamentoRepository _processamentoRepository;
        private readonly ISystemClock _systemClock;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ProcessingOptions _processingOptions;
        private readonly ILogger<ProcessamentoService> _logger;

        public ProcessamentoService(
            IBackgroundTaskQueue backgroundTaskQueue,
            IDocumentoRepository documentoRepository,
            IArquivoStorageService arquivoStorageService,
            IHashArquivoService hashArquivoService,
            IProcessamentoRepository processamentoRepository,
            ISystemClock systemClock,
            IUsuarioAcessoService usuarioAcessoService,
            IOptions<ProcessingOptions> processingOptions,
            ILogger<ProcessamentoService> logger)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _documentoRepository = documentoRepository;
            _arquivoStorageService = arquivoStorageService;
            _hashArquivoService = hashArquivoService;
            _processamentoRepository = processamentoRepository;
            _systemClock = systemClock;
            _usuarioAcessoService = usuarioAcessoService;
            _processingOptions = processingOptions.Value;
            _logger = logger;
        }

        public async Task<ProcessamentoResponseDto> CriarEEnfileirarAsync(UploadProcessamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request.Arquivo is null || request.Arquivo.Length == 0)
            {
                throw new InvalidOperationException("Arquivo é obrigatório.");
            }

            var usuario = await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var documento = await _documentoRepository.GetByIdAsync(request.DocumentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            var extension = Path.GetExtension(request.Arquivo.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Apenas arquivos Excel .xls e .xlsx são aceitos.");
            }

            byte[] content;
            await using (var memoryStream = new MemoryStream())
            {
                await request.Arquivo.CopyToAsync(memoryStream, cancellationToken);
                content = memoryStream.ToArray();
            }

            var hash = _hashArquivoService.ComputeSha256(content);

            if (await _processamentoRepository.ExistsByHashAsync(request.EmpresaId, hash, cancellationToken))
            {
                throw new InvalidOperationException("Arquivo já processado anteriormente para a empresa informada.");
            }

            var fileName = $"{hash}_{Guid.NewGuid():N}{extension}";
            var filePath = await _arquivoStorageService.SaveAsync(fileName, content, cancellationToken);

            var entity = new Processamento
            {
                FK_IdUsuario = usuario.Id,
                FK_IdEmpresa = request.EmpresaId,
                FK_IdDocumento = documento.Id,
                NomeArquivo = request.Arquivo.FileName,
                DataExecucao = _systemClock.UtcNow,
                Status = StatusProcessamento.Processando,
                TotalErro = 0,
                TotalRegistros = 0,
                TotalSucesso = 0,
                HashArquivo = hash
            };

            await _processamentoRepository.AddAsync(entity, cancellationToken);
            await _processamentoRepository.SaveChangesAsync(cancellationToken);

            entity.Documento = documento;

            await _backgroundTaskQueue.EnqueueAsync(new ProcessamentoQueueItem
            {
                ProcessamentoId = entity.Id,
                FilePath = filePath,
                Attempt = 0
            }, cancellationToken);

            _logger.LogInformation("Processamento {ProcessamentoId} criado para empresa {EmpresaId} e documento {DocumentoId}", entity.Id, entity.FK_IdEmpresa, entity.FK_IdDocumento);

            return Map(entity);
        }

        public async Task<ProcessamentoResponseDto> GetByIdAsync(int processamentoId, CancellationToken cancellationToken = default)
        {
            var processamento = await _processamentoRepository.GetByIdAsync(processamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Processamento não encontrado.");

            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(processamento.FK_IdEmpresa, false, cancellationToken);
            return Map(processamento);
        }

        public async Task<PagedResultDto<ProcessamentoResponseDto>> GetPagedAsync(ProcessamentoQueryDto query, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(query.EmpresaId, false, cancellationToken);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, _processingOptions.MaxPageSize);
            var result = await _processamentoRepository.GetPagedAsync(query.EmpresaId, query.Status, pageNumber, pageSize, cancellationToken);

            return new PagedResultDto<ProcessamentoResponseDto>
            {
                Items = result.Items.Select(Map).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResultDto<ProcessamentoItemResponseDto>> GetItemsPagedAsync(int processamentoId, ProcessamentoItensQueryDto query, CancellationToken cancellationToken = default)
        {
            var processamento = await _processamentoRepository.GetByIdAsync(processamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Processamento não encontrado.");

            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(processamento.FK_IdEmpresa, false, cancellationToken);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, _processingOptions.MaxPageSize);
            var result = await _processamentoRepository.GetItemsPagedAsync(processamentoId, query.Status, query.ApenasComErro, pageNumber, pageSize, cancellationToken);

            return new PagedResultDto<ProcessamentoItemResponseDto>
            {
                Items = result.Items.Select(x => new ProcessamentoItemResponseDto
                {
                    Id = x.Id,
                    LinhaExcel = x.LinhaExcel,
                    JsonEnviado = x.JsonEnviado,
                    JsonRetorno = x.JsonRetorno,
                    Erro = x.Erro,
                    Status = x.Status
                }).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task MarcarErroFinalAsync(int processamentoId, Exception exception, CancellationToken cancellationToken = default)
        {
            var processamento = await _processamentoRepository.GetForExecutionAsync(processamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Processamento não encontrado.");

            var erroDetalhado = exception.ToString();

            processamento.Status = StatusProcessamento.Erro;
            processamento.TotalErro = Math.Max(1, processamento.TotalErro);

            await _processamentoRepository.AddItemAsync(new ProcessamentoItem
            {
                FK_IdProcessamento = processamento.Id,
                LinhaExcel = 0,
                JsonEnviado = GetExceptionData(exception, RequestPayloadKey) ?? JsonSerializer.Serialize(new { processamento.Id }),
                JsonRetorno = GetExceptionData(exception, ResponseBodyKey) ?? exception.Message,
                Erro = erroDetalhado.Length > 4000 ? erroDetalhado[..4000] : erroDetalhado,
                Status = StatusProcessamentoItem.Erro
            }, cancellationToken);

            await _processamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private static string? GetExceptionData(Exception exception, string key)
        {
            return exception.Data.Contains(key) ? exception.Data[key]?.ToString() : null;
        }

        private static ProcessamentoResponseDto Map(Processamento processamento)
        {
            return new ProcessamentoResponseDto
            {
                Id = processamento.Id,
                UsuarioId = processamento.FK_IdUsuario,
                EmpresaId = processamento.FK_IdEmpresa,
                DocumentoId = processamento.FK_IdDocumento,
                NomeDocumento = processamento.Documento?.NomeDocumento ?? string.Empty,
                EndpointDocumento = processamento.Documento?.Endpoint ?? string.Empty,
                NomeArquivo = processamento.NomeArquivo,
                DataExecucao = processamento.DataExecucao,
                Status = processamento.Status,
                TotalRegistros = processamento.TotalRegistros,
                TotalSucesso = processamento.TotalSucesso,
                TotalErro = processamento.TotalErro,
                HashArquivo = processamento.HashArquivo
            };
        }
    }
}
