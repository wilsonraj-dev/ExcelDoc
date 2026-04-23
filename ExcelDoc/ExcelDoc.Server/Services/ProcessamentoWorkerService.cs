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
        private readonly IPayloadBuilderService _payloadBuilderService;
        private readonly IProcessamentoRepository _processamentoRepository;
        private readonly ISapServiceLayerClient _sapServiceLayerClient;
        private readonly ILogger<ProcessamentoWorkerService> _logger;

        public ProcessamentoWorkerService(
            IConfiguracaoRepository configuracaoRepository,
            IEncryptionService encryptionService,
            IExcelReaderService excelReaderService,
            IPayloadBuilderService payloadBuilderService,
            IProcessamentoRepository processamentoRepository,
            ISapServiceLayerClient sapServiceLayerClient,
            ILogger<ProcessamentoWorkerService> logger)
        {
            _configuracaoRepository = configuracaoRepository;
            _encryptionService = encryptionService;
            _excelReaderService = excelReaderService;
            _payloadBuilderService = payloadBuilderService;
            _processamentoRepository = processamentoRepository;
            _sapServiceLayerClient = sapServiceLayerClient;
            _logger = logger;
        }

        public async Task ProcessAsync(Background.ProcessamentoQueueItem item, CancellationToken cancellationToken = default)
        {
            var processamento = await _processamentoRepository.GetForExecutionAsync(item.ProcessamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Processamento não encontrado.");

            var configuracao = await _configuracaoRepository.GetByEmpresaIdAsync(processamento.FK_IdEmpresa, cancellationToken)
                ?? throw new InvalidOperationException("Configuração da empresa não encontrada.");

            var sapConfig = DecryptConfiguration(configuracao);
            var sapSession = await _sapServiceLayerClient.LoginAsync(sapConfig, cancellationToken);
            var rows = await _excelReaderService.ReadRowsAsync(item.FilePath, cancellationToken);
            var dataRows = rows.Skip(2).ToList();

            processamento.TotalRegistros = dataRows.Count;
            processamento.TotalErro = 0;
            processamento.TotalSucesso = 0;
            processamento.Status = StatusProcessamento.Processando;
            await _processamentoRepository.SaveChangesAsync(cancellationToken);

            foreach (var row in dataRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemLog = new ProcessamentoItem
                {
                    FK_IdProcessamento = processamento.Id,
                    LinhaExcel = row.RowNumber,
                    JsonEnviado = string.Empty,
                    Status = StatusProcessamentoItem.Erro
                };

                try
                {
                    var payload = _payloadBuilderService.BuildPayload(processamento.Documento, row.Values);
                    var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
                    var responseJson = await _sapServiceLayerClient.PostAsync(sapConfig, sapSession, processamento.Documento.Endpoint, payloadJson, cancellationToken);

                    itemLog.JsonEnviado = payloadJson;
                    itemLog.JsonRetorno = responseJson;
                    itemLog.Status = StatusProcessamentoItem.Sucesso;
                    processamento.TotalSucesso++;
                }
                catch (Exception ex)
                {
                    var errorText = ex.ToString();
                    itemLog.Erro = errorText.Length > 4000 ? errorText[..4000] : errorText;
                    itemLog.JsonEnviado = itemLog.JsonEnviado == string.Empty
                        ? GetExceptionData(ex, RequestPayloadKey) ?? JsonSerializer.Serialize(new { Row = row.RowNumber }, JsonOptions)
                        : itemLog.JsonEnviado;
                    itemLog.JsonRetorno ??= GetExceptionData(ex, ResponseBodyKey) ?? ex.Message;
                    processamento.TotalErro++;

                    _logger.LogError(ex, "Erro ao processar linha {LinhaExcel} do processamento {ProcessamentoId}", row.RowNumber, processamento.Id);
                }

                await _processamentoRepository.AddItemAsync(itemLog, cancellationToken);
                await _processamentoRepository.SaveChangesAsync(cancellationToken);
            }

            processamento.Status = processamento.TotalErro > 0 ? StatusProcessamento.Erro : StatusProcessamento.Sucesso;
            await _processamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processamento {ProcessamentoId} finalizado. Sucesso={TotalSucesso} Erro={TotalErro}", processamento.Id, processamento.TotalSucesso, processamento.TotalErro);
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
                UsuarioBanco = _encryptionService.Decrypt(configuracao.UsuarioBanco),
                SenhaBanco = _encryptionService.Decrypt(configuracao.SenhaBanco),
                UsuarioSAP = _encryptionService.Decrypt(configuracao.UsuarioSAP),
                SenhaSAP = _encryptionService.Decrypt(configuracao.SenhaSAP)
            };
        }
    }
}
