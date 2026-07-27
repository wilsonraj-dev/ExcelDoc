using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Repositories;

public sealed class ProcessamentoRepository : IProcessamentoRepository
{
    private readonly ISapUdtStore _store;
    private readonly List<Processamento> _pendingProcesses = [];
    private readonly List<ProcessamentoItem> _pendingItems = [];
    private readonly Dictionary<int, Processamento> _trackedProcesses = [];

    public ProcessamentoRepository(ISapUdtStore store)
    {
        _store = store;
    }

    public Task AddAsync(
        Processamento processamento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingProcesses.Add(processamento);
        return Task.CompletedTask;
    }

    public async Task<Processamento?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Processamento,
            id,
            cancellationToken);
        if (record is null)
        {
            return null;
        }

        var process = SapEntityMapper.ToProcessamento(record);
        await HydrateDisplayReferencesAsync([process], cancellationToken);
        return process;
    }

    public async Task<Processamento?> GetForExecutionAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Processamento,
            id,
            cancellationToken);
        if (record is null)
        {
            return null;
        }

        var process = SapEntityMapper.ToProcessamento(record);
        var hydrator = new SapDataHydrator(_store);
        var documents = (await hydrator.LoadDocumentosAsync(
                includeLinks: true,
                includeCollectionGraph: true,
                cancellationToken))
            .ToDictionary(document => document.Id);
        var profiles = (await hydrator.LoadPerfisAsync(
                includeGraph: true,
                cancellationToken))
            .ToDictionary(profile => profile.Id);
        if (documents.TryGetValue(process.FK_IdDocumento, out var document))
        {
            process.Documento = document;
        }

        if (process.FK_IdPerfilMapeamento.HasValue &&
            profiles.TryGetValue(process.FK_IdPerfilMapeamento.Value, out var profile))
        {
            process.PerfilMapeamento = profile;
        }

        _trackedProcesses[process.Id] = process;
        return process;
    }

    public async Task<(IReadOnlyCollection<Processamento> Items, int TotalCount)> GetPagedAsync(
        StatusProcessamento? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var profiles = (await _store.QueryAsync(
                SapUdtSchema.PerfilMapeamento,
                cancellationToken: cancellationToken))
            .Select(SapEntityMapper.ToPerfilMapeamento)
            .ToDictionary(profile => profile.Id);
        var filter = status.HasValue
            ? SapOData.Eq("Status", (int)status.Value)
            : null;
        var totalCount = await _store.CountAsync(
            SapUdtSchema.Processamento,
            filter,
            cancellationToken);
        var page = (await _store.QueryAsync(
                SapUdtSchema.Processamento,
                filter: filter,
                orderBy: $"{SapOData.Field("DataExecucao")} desc,Code desc",
                top: pageSize,
                skip: (pageNumber - 1) * pageSize,
                cancellationToken: cancellationToken))
            .Select(SapEntityMapper.ToProcessamento)
            .ToList();

        await HydrateDisplayReferencesAsync(page, cancellationToken, profiles);
        return (page, totalCount);
    }

    public async Task<(IReadOnlyCollection<ProcessamentoItem> Items, int TotalCount)> GetItemsPagedAsync(
        int processamentoId,
        StatusProcessamentoItem? status,
        bool apenasComErro,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (apenasComErro &&
            status.HasValue &&
            status.Value != StatusProcessamentoItem.Erro)
        {
            return (Array.Empty<ProcessamentoItem>(), 0);
        }

        var effectiveStatus = apenasComErro
            ? StatusProcessamentoItem.Erro
            : status;
        var filter = SapOData.And(
            SapOData.Eq("ProcessamentoId", processamentoId),
            effectiveStatus.HasValue
                ? SapOData.Eq("Status", (int)effectiveStatus.Value)
                : null);
        var totalCount = await _store.CountAsync(
            SapUdtSchema.ProcessamentoItem,
            filter,
            cancellationToken);
        var items = (await _store.QueryAsync(
                SapUdtSchema.ProcessamentoItem,
                filter: filter,
                orderBy: $"{SapOData.Field("LinhaExcel")} asc,Code asc",
                top: pageSize,
                skip: (pageNumber - 1) * pageSize,
                cancellationToken: cancellationToken))
            .Select(SapEntityMapper.ToProcessamentoItem)
            .ToList();

        return (items, totalCount);
    }

    public Task AddItemAsync(
        ProcessamentoItem item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingItems.Add(item);
        return Task.CompletedTask;
    }

    public async Task<bool> HasDocumentoProcessadoComSucessoAsync(
        string idDocumentoUnico,
        CancellationToken cancellationToken = default)
    {
        var records = await _store.QueryAsync(
            SapUdtSchema.ProcessamentoItem,
            filter: SapOData.And(
                SapOData.Eq("IdDocumentoUnico", idDocumentoUnico),
                SapOData.Eq("Status", (int)StatusProcessamentoItem.Sucesso)),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);
        return records.Count > 0;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var insertedProcessIds = new HashSet<int>();
        foreach (var process in _pendingProcesses)
        {
            process.Id = await _store.AddAsync(
                SapUdtSchema.Processamento,
                SapEntityMapper.Fields(process),
                cancellationToken: cancellationToken);
            insertedProcessIds.Add(process.Id);
            _trackedProcesses[process.Id] = process;
        }

        _pendingProcesses.Clear();

        foreach (var item in _pendingItems)
        {
            if (item.FK_IdProcessamento <= 0 &&
                item.Processamento is not null)
            {
                item.FK_IdProcessamento = item.Processamento.Id;
            }

            item.Id = await _store.AddAsync(
                SapUdtSchema.ProcessamentoItem,
                SapEntityMapper.Fields(item),
                cancellationToken: cancellationToken);
        }

        _pendingItems.Clear();

        foreach (var process in _trackedProcesses.Values.Where(
                     value => !insertedProcessIds.Contains(value.Id)))
        {
            await _store.UpdateAsync(
                SapUdtSchema.Processamento,
                process.Id,
                SapEntityMapper.Fields(process),
                cancellationToken);
        }
    }

    private async Task HydrateDisplayReferencesAsync(
        IReadOnlyCollection<Processamento> processes,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<int, PerfilMapeamento>? loadedProfiles = null)
    {
        if (processes.Count == 0)
        {
            return;
        }

        var hydrator = new SapDataHydrator(_store);
        var documents = (await hydrator.LoadDocumentosAsync(
                includeLinks: false,
                includeCollectionGraph: false,
                cancellationToken))
            .ToDictionary(document => document.Id);
        var profiles = loadedProfiles ??
                       (await hydrator.LoadPerfisAsync(
                               includeGraph: false,
                               cancellationToken))
                       .ToDictionary(profile => profile.Id);

        foreach (var process in processes)
        {
            if (documents.TryGetValue(process.FK_IdDocumento, out var document))
            {
                process.Documento = document;
            }

            if (process.FK_IdPerfilMapeamento.HasValue &&
                profiles.TryGetValue(
                    process.FK_IdPerfilMapeamento.Value,
                    out var profile))
            {
                process.PerfilMapeamento = profile;
            }
        }
    }
}
