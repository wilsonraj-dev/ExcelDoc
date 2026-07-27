using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Sap;

internal sealed class SapDataHydrator
{
    private readonly ISapUdtStore _store;

    public SapDataHydrator(ISapUdtStore store)
    {
        _store = store;
    }

    public async Task<List<Documento>> LoadDocumentosAsync(
        bool includeLinks,
        bool includeCollectionGraph,
        CancellationToken cancellationToken = default)
    {
        var documentRows = await _store.QueryAsync(
            SapUdtSchema.Documento,
            cancellationToken: cancellationToken);
        var documents = documentRows
            .Select(SapEntityMapper.ToDocumento)
            .ToDictionary(document => document.Id);

        if (!includeLinks || documents.Count == 0)
        {
            return documents.Values.ToList();
        }

        var linkRows = await _store.QueryAsync(
            SapUdtSchema.DocumentoColecao,
            cancellationToken: cancellationToken);
        Dictionary<int, Colecao>? collections = null;

        if (includeCollectionGraph)
        {
            collections = (await LoadColecoesAsync(
                    includeMappings: true,
                    includeDocuments: false,
                    cancellationToken))
                .ToDictionary(collection => collection.Id);
        }

        foreach (var row in linkRows)
        {
            var link = SapEntityMapper.ToDocumentoColecao(row);
            if (!documents.TryGetValue(link.FK_IdDocumento, out var document))
            {
                continue;
            }

            link.Documento = document;
            if (collections is not null &&
                collections.TryGetValue(link.FK_IdColecao, out var collection))
            {
                link.Colecao = collection;
            }

            document.DocumentoColecoes.Add(link);
        }

        return documents.Values.ToList();
    }

    public async Task<List<Colecao>> LoadColecoesAsync(
        bool includeMappings,
        bool includeDocuments,
        CancellationToken cancellationToken = default)
    {
        var collectionRows = await _store.QueryAsync(
            SapUdtSchema.Colecao,
            cancellationToken: cancellationToken);
        var collections = collectionRows
            .Select(SapEntityMapper.ToColecao)
            .ToDictionary(collection => collection.Id);

        if (includeMappings && collections.Count > 0)
        {
            var mappingRows = await _store.QueryAsync(
                SapUdtSchema.Mapeamento,
                cancellationToken: cancellationToken);
            var mappings = mappingRows
                .Select(SapEntityMapper.ToMapeamento)
                .Where(mapping => collections.ContainsKey(mapping.FK_IdColecao))
                .ToDictionary(mapping => mapping.Id);
            var fieldRows = await _store.QueryAsync(
                SapUdtSchema.MapeamentoCampo,
                cancellationToken: cancellationToken);

            foreach (var field in fieldRows.Select(SapEntityMapper.ToMapeamentoCampo))
            {
                if (mappings.TryGetValue(field.FK_IdMapeamento, out var mapping))
                {
                    field.Mapeamento = mapping;
                    mapping.Campos.Add(field);
                }
            }

            foreach (var mapping in mappings.Values)
            {
                var collection = collections[mapping.FK_IdColecao];
                mapping.Colecao = collection;
                mapping.Campos = mapping.Campos
                    .OrderBy(field => field.IndiceColuna)
                    .ToList();
                collection.Mapeamentos.Add(mapping);
            }
        }

        if (includeDocuments && collections.Count > 0)
        {
            var documentRows = await _store.QueryAsync(
                SapUdtSchema.Documento,
                cancellationToken: cancellationToken);
            var documents = documentRows
                .Select(SapEntityMapper.ToDocumento)
                .ToDictionary(document => document.Id);
            var linkRows = await _store.QueryAsync(
                SapUdtSchema.DocumentoColecao,
                cancellationToken: cancellationToken);

            foreach (var row in linkRows)
            {
                var link = SapEntityMapper.ToDocumentoColecao(row);
                if (!collections.TryGetValue(link.FK_IdColecao, out var collection) ||
                    !documents.TryGetValue(link.FK_IdDocumento, out var document))
                {
                    continue;
                }

                link.Colecao = collection;
                link.Documento = document;
                collection.DocumentoColecoes.Add(link);
                document.DocumentoColecoes.Add(link);
            }
        }

        return collections.Values.ToList();
    }

    public async Task<List<Mapeamento>> LoadMapeamentosAsync(
        bool includeColecao,
        bool includeCampos,
        CancellationToken cancellationToken = default)
    {
        var mappingRows = await _store.QueryAsync(
            SapUdtSchema.Mapeamento,
            cancellationToken: cancellationToken);
        var mappings = mappingRows
            .Select(SapEntityMapper.ToMapeamento)
            .ToDictionary(mapping => mapping.Id);

        if (includeColecao && mappings.Count > 0)
        {
            var collectionRows = await _store.QueryAsync(
                SapUdtSchema.Colecao,
                cancellationToken: cancellationToken);
            var collections = collectionRows
                .Select(SapEntityMapper.ToColecao)
                .ToDictionary(collection => collection.Id);

            foreach (var mapping in mappings.Values)
            {
                if (collections.TryGetValue(mapping.FK_IdColecao, out var collection))
                {
                    mapping.Colecao = collection;
                    collection.Mapeamentos.Add(mapping);
                }
            }
        }

        if (includeCampos && mappings.Count > 0)
        {
            var fieldRows = await _store.QueryAsync(
                SapUdtSchema.MapeamentoCampo,
                cancellationToken: cancellationToken);
            foreach (var field in fieldRows.Select(SapEntityMapper.ToMapeamentoCampo))
            {
                if (!mappings.TryGetValue(field.FK_IdMapeamento, out var mapping))
                {
                    continue;
                }

                field.Mapeamento = mapping;
                mapping.Campos.Add(field);
            }

            foreach (var mapping in mappings.Values)
            {
                mapping.Campos = mapping.Campos
                    .OrderBy(field => field.IndiceColuna)
                    .ToList();
            }
        }

        return mappings.Values.ToList();
    }

    public async Task<List<DocumentoColecao>> LoadDocumentoColecoesAsync(
        CancellationToken cancellationToken = default)
    {
        var collectionRows = await _store.QueryAsync(
            SapUdtSchema.Colecao,
            cancellationToken: cancellationToken);
        var collections = collectionRows
            .Select(SapEntityMapper.ToColecao)
            .ToDictionary(collection => collection.Id);
        var documentRows = await _store.QueryAsync(
            SapUdtSchema.Documento,
            cancellationToken: cancellationToken);
        var documents = documentRows
            .Select(SapEntityMapper.ToDocumento)
            .ToDictionary(document => document.Id);
        var linkRows = await _store.QueryAsync(
            SapUdtSchema.DocumentoColecao,
            cancellationToken: cancellationToken);
        var links = new List<DocumentoColecao>();

        foreach (var row in linkRows)
        {
            var link = SapEntityMapper.ToDocumentoColecao(row);
            if (!collections.TryGetValue(link.FK_IdColecao, out var collection) ||
                !documents.TryGetValue(link.FK_IdDocumento, out var document))
            {
                continue;
            }

            link.Colecao = collection;
            link.Documento = document;
            links.Add(link);
        }

        return links;
    }

    public async Task<List<PerfilMapeamento>> LoadPerfisAsync(
        bool includeGraph,
        CancellationToken cancellationToken = default)
    {
        var profileRows = await _store.QueryAsync(
            SapUdtSchema.PerfilMapeamento,
            cancellationToken: cancellationToken);
        var profiles = profileRows
            .Select(SapEntityMapper.ToPerfilMapeamento)
            .ToDictionary(profile => profile.Id);
        var documentRows = await _store.QueryAsync(
            SapUdtSchema.Documento,
            cancellationToken: cancellationToken);
        var documents = documentRows
            .Select(SapEntityMapper.ToDocumento)
            .ToDictionary(document => document.Id);

        foreach (var profile in profiles.Values)
        {
            if (documents.TryGetValue(profile.FK_IdDocumento, out var document))
            {
                profile.Documento = document;
            }
        }

        if (!includeGraph || profiles.Count == 0)
        {
            return profiles.Values.ToList();
        }

        var collectionRows = await _store.QueryAsync(
            SapUdtSchema.Colecao,
            cancellationToken: cancellationToken);
        var collections = collectionRows
            .Select(SapEntityMapper.ToColecao)
            .ToDictionary(collection => collection.Id);
        var mappingRows = await _store.QueryAsync(
            SapUdtSchema.Mapeamento,
            cancellationToken: cancellationToken);
        var mappings = mappingRows
            .Select(SapEntityMapper.ToMapeamento)
            .ToDictionary(mapping => mapping.Id);
        var fieldRows = await _store.QueryAsync(
            SapUdtSchema.MapeamentoCampo,
            cancellationToken: cancellationToken);

        foreach (var mapping in mappings.Values)
        {
            if (collections.TryGetValue(mapping.FK_IdColecao, out var collection))
            {
                mapping.Colecao = collection;
            }
        }

        foreach (var field in fieldRows.Select(SapEntityMapper.ToMapeamentoCampo))
        {
            if (mappings.TryGetValue(field.FK_IdMapeamento, out var mapping))
            {
                field.Mapeamento = mapping;
                mapping.Campos.Add(field);
            }
        }

        foreach (var mapping in mappings.Values)
        {
            mapping.Campos = mapping.Campos
                .OrderBy(field => field.IndiceColuna)
                .ToList();
        }

        var itemRows = await _store.QueryAsync(
            SapUdtSchema.PerfilMapeamentoItem,
            cancellationToken: cancellationToken);
        var items = itemRows
            .Select(SapEntityMapper.ToPerfilMapeamentoItem)
            .Where(item => profiles.ContainsKey(item.FK_IdPerfilMapeamento))
            .ToDictionary(item => item.Id);

        foreach (var item in items.Values)
        {
            var profile = profiles[item.FK_IdPerfilMapeamento];
            item.PerfilMapeamento = profile;

            if (collections.TryGetValue(item.FK_IdColecao, out var collection))
            {
                item.Colecao = collection;
            }

            if (mappings.TryGetValue(item.FK_IdMapeamento, out var mapping))
            {
                item.Mapeamento = mapping;
            }

            profile.Itens.Add(item);
        }

        foreach (var item in items.Values)
        {
            if (item.FK_IdPerfilMapeamentoItemPai.HasValue &&
                items.TryGetValue(item.FK_IdPerfilMapeamentoItemPai.Value, out var parent))
            {
                item.ItemPai = parent;
                parent.ItensFilhos.Add(item);
            }
        }

        return profiles.Values.ToList();
    }
}
