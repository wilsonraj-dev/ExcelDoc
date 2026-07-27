using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Repositories;

public sealed class ColecaoRepository : IColecaoRepository
{
    private readonly ISapUdtStore _store;
    private readonly List<Colecao> _pendingAdds = [];
    private readonly Dictionary<int, Colecao> _tracked = [];
    private readonly HashSet<int> _pendingDeletes = [];

    public ColecaoRepository(ISapUdtStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyCollection<Colecao>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var collections = await new SapDataHydrator(_store)
            .LoadColecoesAsync(
                includeMappings: true,
                includeDocuments: true,
                cancellationToken);

        return collections
            .OrderBy(collection => collection.NomeColecao)
            .ToList();
    }

    public async Task<Colecao?> GetByIdWithMappingsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var collections = await new SapDataHydrator(_store)
            .LoadColecoesAsync(
                includeMappings: true,
                includeDocuments: true,
                cancellationToken);
        var collection = collections.FirstOrDefault(value => value.Id == id);
        if (collection is not null)
        {
            _tracked[collection.Id] = collection;
        }

        return collection;
    }

    public async Task<bool> ExistsByNomeAsync(
        string nomeColecao,
        TipoColecao tipoColecao,
        int? ignoreId = null,
        CancellationToken cancellationToken = default)
    {
        var records = await _store.QueryAsync(
            SapUdtSchema.Colecao,
            filter: SapOData.Eq("TipoColecao", (int)tipoColecao),
            cancellationToken: cancellationToken);

        return records.Any(record =>
            (!ignoreId.HasValue || record.Id != ignoreId.Value) &&
            string.Equals(
                record.GetString("NomeColecao"),
                nomeColecao,
                StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<Documento>> GetDocumentosByIdsAsync(
        IReadOnlyCollection<int> documentoIds,
        CancellationToken cancellationToken = default)
    {
        if (documentoIds.Count == 0)
        {
            return Array.Empty<Documento>();
        }

        var ids = documentoIds.ToHashSet();
        var documents = await new SapDataHydrator(_store)
            .LoadDocumentosAsync(
                includeLinks: false,
                includeCollectionGraph: false,
                cancellationToken);
        return documents
            .Where(document => ids.Contains(document.Id))
            .ToList();
    }

    public Task AddAsync(
        Colecao colecao,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingAdds.Add(colecao);
        return Task.CompletedTask;
    }

    public void Remove(Colecao colecao)
    {
        if (colecao.Id > 0)
        {
            _pendingDeletes.Add(colecao.Id);
        }
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var collectionId in _pendingDeletes)
        {
            await EnsureCanDeleteAsync(collectionId, cancellationToken);
            await _store.DeleteAsync(
                SapUdtSchema.Colecao,
                collectionId,
                cancellationToken);
            _tracked.Remove(collectionId);
        }

        _pendingDeletes.Clear();

        var insertedIds = new HashSet<int>();
        foreach (var collection in _pendingAdds)
        {
            collection.Id = await _store.AddAsync(
                SapUdtSchema.Colecao,
                SapEntityMapper.Fields(collection),
                cancellationToken: cancellationToken);
            insertedIds.Add(collection.Id);
            _tracked[collection.Id] = collection;

            foreach (var link in collection.DocumentoColecoes)
            {
                link.FK_IdColecao = collection.Id;
                link.Id = await _store.AddAsync(
                    SapUdtSchema.DocumentoColecao,
                    SapEntityMapper.Fields(link),
                    cancellationToken: cancellationToken);
            }
        }

        _pendingAdds.Clear();

        foreach (var collection in _tracked.Values.Where(
                     value => !insertedIds.Contains(value.Id)))
        {
            await _store.UpdateAsync(
                SapUdtSchema.Colecao,
                collection.Id,
                SapEntityMapper.Fields(collection),
                cancellationToken);
            await SynchronizeDocumentLinksAsync(collection, cancellationToken);
        }
    }

    private async Task SynchronizeDocumentLinksAsync(
        Colecao collection,
        CancellationToken cancellationToken)
    {
        var currentRows = await _store.QueryAsync(
            SapUdtSchema.DocumentoColecao,
            filter: SapOData.Eq("ColecaoId", collection.Id),
            cancellationToken: cancellationToken);
        var currentLinks = currentRows
            .Select(SapEntityMapper.ToDocumentoColecao)
            .ToList();
        var desiredDocumentIds = collection.DocumentoColecoes
            .Select(link => link.FK_IdDocumento)
            .Distinct()
            .ToHashSet();

        foreach (var current in currentLinks.Where(
                     link => !desiredDocumentIds.Contains(link.FK_IdDocumento)))
        {
            await _store.DeleteAsync(
                SapUdtSchema.DocumentoColecao,
                current.Id,
                cancellationToken);
        }

        var currentDocumentIds = currentLinks
            .Select(link => link.FK_IdDocumento)
            .ToHashSet();
        foreach (var desired in collection.DocumentoColecoes.Where(
                     link => !currentDocumentIds.Contains(link.FK_IdDocumento)))
        {
            desired.FK_IdColecao = collection.Id;
            desired.Id = await _store.AddAsync(
                SapUdtSchema.DocumentoColecao,
                SapEntityMapper.Fields(desired),
                cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureCanDeleteAsync(
        int collectionId,
        CancellationToken cancellationToken)
    {
        var documentLinks = await _store.QueryAsync(
            SapUdtSchema.DocumentoColecao,
            filter: SapOData.Eq("ColecaoId", collectionId),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);
        var mappings = await _store.QueryAsync(
            SapUdtSchema.Mapeamento,
            filter: SapOData.Eq("ColecaoId", collectionId),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);
        var profileItems = await _store.QueryAsync(
            SapUdtSchema.PerfilMapeamentoItem,
            filter: SapOData.Eq("ColecaoId", collectionId),
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);

        if (documentLinks.Count > 0 || mappings.Count > 0 || profileItems.Count > 0)
        {
            throw new InvalidOperationException(
                "A colecao possui documentos, mapeamentos ou perfis vinculados.");
        }
    }
}
