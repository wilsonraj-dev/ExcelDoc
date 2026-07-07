using System.Text.Json;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class ProcessamentoWorkerService : IProcessamentoWorkerService
    {
        private const string RequestPayloadKey = "RequestPayload";
        private const string ResponseBodyKey = "ResponseBody";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IConfiguracaoRepository _configuracaoRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IExcelReaderService _excelReaderService;
        private readonly IJsonBuilderService _jsonBuilderService;
        private readonly IDocumentoUnicoService _documentoUnicoService;
        private readonly IAgrupamentoService _agrupamentoService;
        private readonly IProcessamentoRepository _processamentoRepository;
        private readonly ISapServiceLayerClient _sapServiceLayerClient;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<ProcessamentoWorkerService> _logger;

        public ProcessamentoWorkerService(
            IConfiguracaoRepository configuracaoRepository,
            IEncryptionService encryptionService,
            IExcelReaderService excelReaderService,
            IJsonBuilderService jsonBuilderService,
            IDocumentoUnicoService documentoUnicoService,
            IAgrupamentoService agrupamentoService,
            IProcessamentoRepository processamentoRepository,
            ISapServiceLayerClient sapServiceLayerClient,
            ISystemClock systemClock,
            ILogger<ProcessamentoWorkerService> logger)
        {
            _configuracaoRepository = configuracaoRepository;
            _encryptionService = encryptionService;
            _excelReaderService = excelReaderService;
            _jsonBuilderService = jsonBuilderService;
            _documentoUnicoService = documentoUnicoService;
            _agrupamentoService = agrupamentoService;
            _processamentoRepository = processamentoRepository;
            _sapServiceLayerClient = sapServiceLayerClient;
            _systemClock = systemClock;
            _logger = logger;
        }

        public async Task ProcessAsync(Background.ProcessamentoQueueItem item, CancellationToken cancellationToken = default)
        {
            var processamento = await _processamentoRepository.GetForExecutionAsync(item.ProcessamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Processamento não encontrado.");

            var configuracao = await _configuracaoRepository.GetByEmpresaIdAsync(processamento.FK_IdEmpresa, cancellationToken)
                ?? throw new InvalidOperationException("Configuração da empresa não encontrada.");

            var sapConfig = DecryptConfiguration(configuracao);
            var rows = await _excelReaderService.ReadRowsAsync(item.FilePath, cancellationToken);

            if (processamento.PerfilMapeamento is null)
            {
                throw new InvalidOperationException("Processamento sem perfil de mapeamento não é suportado.");
            }

            IReadOnlyList<Background.ExcelDocumentGroup> groups;
            try
            {
                groups = _agrupamentoService.AgruparPorIdExcel(rows);
            }
            catch (ExcelImportValidationException ex)
            {
                await RegisterValidationErrorAsync(processamento, ex, cancellationToken);
                return;
            }

            await ProcessWithPerfilAsync(processamento, sapConfig, groups, cancellationToken);

            _logger.LogInformation("Processamento {ProcessamentoId} finalizado. Sucesso={TotalSucesso} Erro={TotalErro} Ignorado={TotalIgnorado}",
                processamento.Id, processamento.TotalSucesso, processamento.TotalErro, processamento.TotalIgnorado);
        }

        private async Task ProcessWithPerfilAsync(
            Processamento processamento,
            Configuracao sapConfig,
            IReadOnlyList<Background.ExcelDocumentGroup> groups,
            CancellationToken cancellationToken)
        {
            var perfil = processamento.PerfilMapeamento!;
            Background.SapSession? sapSession = null;

            processamento.TotalRegistros = groups.Count;
            processamento.TotalErro = 0;
            processamento.TotalSucesso = 0;
            processamento.TotalIgnorado = 0;
            processamento.Status = StatusProcessamento.Processando;
            await _processamentoRepository.SaveChangesAsync(cancellationToken);

            foreach (var group in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var groupRows = group.Rows;
                var firstRowNumber = groupRows[0].RowNumber;
                var itemLog = new ProcessamentoItem
                {
                    FK_IdProcessamento = processamento.Id,
                    IdExcel = group.IdExcel,
                    LinhaExcel = firstRowNumber,
                    JsonEnviado = string.Empty,
                    Status = StatusProcessamentoItem.Erro,
                    DataExecucao = _systemClock.UtcNow
                };

                try
                {
                    var payload = _jsonBuilderService.BuildDocumentPayload(perfil, groupRows);
                    var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
                    var idDocumentoUnico = _documentoUnicoService.BuildIdDocumentoUnico(processamento, group, payload);
                    itemLog.JsonEnviado = payloadJson;
                    itemLog.IdDocumentoUnico = idDocumentoUnico;

                    if (await _processamentoRepository.HasDocumentoProcessadoComSucessoAsync(idDocumentoUnico, cancellationToken))
                    {
                        itemLog.JsonRetorno = JsonSerializer.Serialize(new
                        {
                            Ignorado = true,
                            Motivo = "Documento já importado com sucesso.",
                            group.IdExcel,
                            IdDocumentoUnico = idDocumentoUnico
                        }, JsonOptions);
                        itemLog.Mensagem = "Documento ignorado porque já existe importação anterior com sucesso para o mesmo identificador.";
                        itemLog.Status = StatusProcessamentoItem.Ignorado;
                        processamento.TotalIgnorado++;

                        _logger.LogInformation("Documento IdExcel {IdExcel} ignorado por duplicidade no processamento {ProcessamentoId}. IdDocumentoUnico={IdDocumentoUnico}",
                            group.IdExcel, processamento.Id, idDocumentoUnico);
                    }
                    else
                    {
                        sapSession ??= await _sapServiceLayerClient.LoginAsync(sapConfig, cancellationToken);

                        var responseJson = await _sapServiceLayerClient.PostAsync(
                            sapConfig, sapSession, processamento.Documento.Endpoint, payloadJson, cancellationToken);

                        itemLog.JsonRetorno = responseJson;
                        itemLog.Mensagem = "Documento processado com sucesso.";
                        itemLog.Status = StatusProcessamentoItem.Sucesso;
                        processamento.TotalSucesso++;
                    }
                }
                catch (Exception ex)
                {
                    var errorText = ex.ToString();
                    itemLog.Erro = errorText.Length > 4000 ? errorText[..4000] : errorText;
                    itemLog.JsonEnviado = itemLog.JsonEnviado == string.Empty
                        ? GetExceptionData(ex, RequestPayloadKey) ?? JsonSerializer.Serialize(new { group.IdExcel, FirstRow = firstRowNumber, RowCount = groupRows.Count }, JsonOptions)
                        : itemLog.JsonEnviado;
                    itemLog.JsonRetorno ??= GetExceptionData(ex, ResponseBodyKey) ?? ex.Message;
                    itemLog.Mensagem = ex.Message;
                    processamento.TotalErro++;

                    _logger.LogError(ex, "Erro ao processar IdExcel {IdExcel} (linha inicial {LinhaExcel}) do processamento {ProcessamentoId}",
                        group.IdExcel, firstRowNumber, processamento.Id);
                }

                itemLog.DataFinalizacao = _systemClock.UtcNow;
                await _processamentoRepository.AddItemAsync(itemLog, cancellationToken);
                await _processamentoRepository.SaveChangesAsync(cancellationToken);
            }

            processamento.Status = processamento.TotalErro > 0 ? StatusProcessamento.Erro : StatusProcessamento.Sucesso;
            await _processamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task RegisterValidationErrorAsync(
            Processamento processamento,
            ExcelImportValidationException exception,
            CancellationToken cancellationToken)
        {
            processamento.Status = StatusProcessamento.Erro;
            processamento.TotalRegistros = 0;
            processamento.TotalSucesso = 0;
            processamento.TotalErro = 1;
            processamento.TotalIgnorado = 0;

            var errorText = exception.ToString();
            var now = _systemClock.UtcNow;
            await _processamentoRepository.AddItemAsync(new ProcessamentoItem
            {
                FK_IdProcessamento = processamento.Id,
                LinhaExcel = exception.RowNumber ?? 0,
                JsonEnviado = JsonSerializer.Serialize(new
                {
                    Validation = "IdExcel",
                    Column = "#",
                    exception.RowNumber
                }, JsonOptions),
                JsonRetorno = exception.Message,
                Mensagem = exception.Message,
                Erro = errorText.Length > 4000 ? errorText[..4000] : errorText,
                Status = StatusProcessamentoItem.Erro,
                DataExecucao = now,
                DataFinalizacao = now
            }, cancellationToken);

            await _processamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(exception, "Erro de validacao da planilha no processamento {ProcessamentoId}. LinhaExcel={LinhaExcel}",
                processamento.Id, exception.RowNumber);
        }

        private static string? GetExceptionData(Exception exception, string key)
        {
            return exception.Data.Contains(key) ? exception.Data[key]?.ToString() : null;
        }

        private Configuracao DecryptConfiguration(Configuracao configuracao)
        {
            return new Configuracao
            {
                Id = configuracao.Id,
                FK_IdEmpresa = configuracao.FK_IdEmpresa,
                LinkServiceLayer = configuracao.LinkServiceLayer,
                Database = configuracao.Database,
                UsuarioSAP = _encryptionService.Decrypt(configuracao.UsuarioSAP),
                SenhaSAP = _encryptionService.Decrypt(configuracao.SenhaSAP)
            };
        }
    }
}
