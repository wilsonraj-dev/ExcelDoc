using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Repositories;

public sealed class DocumentoRepository : IDocumentoRepository
{
    private readonly ISapUdtStore _store;
    private readonly List<Documento> _pendingAdds = [];
    private readonly Dictionary<int, Documento> _tracked = [];
    private readonly HashSet<int> _pendingDeletes = [];

    public DocumentoRepository(ISapUdtStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyCollection<Documento>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var documents = await new SapDataHydrator(_store)
            .LoadDocumentosAsync(
                includeLinks: true,
                includeCollectionGraph: false,
                cancellationToken);
        return documents
            .OrderBy(document => document.NomeDocumento)
            .ToList();
    }

    public async Task<Documento?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var documents = await new SapDataHydrator(_store)
            .LoadDocumentosAsync(
                includeLinks: true,
                includeCollectionGraph: false,
                cancellationToken);
        return documents.FirstOrDefault(document => document.Id == id);
    }

    public async Task<Documento?> GetTrackedByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Documento,
            id,
            cancellationToken);
        if (record is null)
        {
            return null;
        }

        var document = SapEntityMapper.ToDocumento(record);
        _tracked[document.Id] = document;
        return document;
    }

    public async Task<Documento?> GetForProcessingAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var documents = await new SapDataHydrator(_store)
            .LoadDocumentosAsync(
                includeLinks: true,
                includeCollectionGraph: true,
                cancellationToken);
        var document = documents.FirstOrDefault(value => value.Id == id);
        if (document is not null)
        {
            _tracked[document.Id] = document;
        }

        return document;
    }

    public async Task<bool> ExistsByNomeOrEndpointAsync(
        string nomeDocumento,
        string endpoint,
        int? ignoreId = null,
        CancellationToken cancellationToken = default)
    {
        var records = await _store.QueryAsync(
            SapUdtSchema.Documento,
            cancellationToken: cancellationToken);

        return records
            .Select(SapEntityMapper.ToDocumento)
            .Any(document =>
                (!ignoreId.HasValue || document.Id != ignoreId.Value) &&
                (string.Equals(
                     document.NomeDocumento,
                     nomeDocumento,
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     document.Endpoint,
                     endpoint,
                     StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(
        Documento documento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingAdds.Add(documento);
        return Task.CompletedTask;
    }

    public void Remove(Documento documento)
    {
        if (documento.Id > 0)
        {
            _pendingDeletes.Add(documento.Id);
        }
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var documentId in _pendingDeletes)
        {
            await EnsureCanDeleteAsync(documentId, cancellationToken);

            var links = await _store.QueryAsync(
                SapUdtSchema.DocumentoColecao,
                filter: SapOData.Eq("DocumentoId", documentId),
                cancellationToken: cancellationToken);
            foreach (var link in links)
            {
                await _store.DeleteAsync(
                    SapUdtSchema.DocumentoColecao,
                    link.Id,
                    cancellationToken);
            }

            await _store.DeleteAsync(
                SapUdtSchema.Documento,
                documentId,
                cancellationToken);
            _tracked.Remove(documentId);
        }

        _pendingDeletes.Clear();

        var insertedIds = new HashSet<int>();
        foreach (var document in _pendingAdds)
        {
            document.Id = await _store.AddAsync(
                SapUdtSchema.Documento,
                SapEntityMapper.Fields(document),
                cancellationToken: cancellationToken);
            _tracked[document.Id] = document;
            insertedIds.Add(document.Id);
        }

        _pendingAdds.Clear();

        foreach (var document in _tracked.Values.Where(
                     document =>
                         !insertedIds.Contains(document.Id) &&
                         !_pendingDeletes.Contains(document.Id)))
        {
            await _store.UpdateAsync(
                SapUdtSchema.Documento,
                document.Id,
                SapEntityMapper.Fields(document),
                cancellationToken);
        }
    }

    private async Task EnsureCanDeleteAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        var profiles = await _store.QueryAsync(
            SapUdtSchema.PerfilMapeamento,
            filter: SapOData.Eq("DocumentoId", documentId),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);
        var processes = await _store.QueryAsync(
            SapUdtSchema.Processamento,
            filter: SapOData.Eq("DocumentoId", documentId),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);

        if (profiles.Count > 0 || processes.Count > 0)
        {
            throw new InvalidOperationException(
                "O documento possui perfis ou processamentos vinculados.");
        }
    }
}
