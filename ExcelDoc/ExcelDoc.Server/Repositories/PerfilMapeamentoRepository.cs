using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Repositories;

public sealed class PerfilMapeamentoRepository : IPerfilMapeamentoRepository
{
    private readonly ISapUdtStore _store;
    private readonly List<PerfilMapeamento> _pendingAdds = [];
    private readonly Dictionary<int, TrackedProfile> _tracked = [];
    private readonly HashSet<int> _pendingDeletes = [];

    public PerfilMapeamentoRepository(ISapUdtStore store)
    {
        _store = store;
    }

    public async Task<PerfilMapeamento?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var profiles = await new SapDataHydrator(_store)
            .LoadPerfisAsync(includeGraph: true, cancellationToken);
        var profile = profiles.FirstOrDefault(value => value.Id == id);
        if (profile is not null)
        {
            _tracked[profile.Id] = new TrackedProfile(
                profile,
                profile.Itens.Select(item => item.Id).ToHashSet());
        }

        return profile;
    }

    public async Task<PerfilMapeamento?> GetForExecutionAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var profiles = await new SapDataHydrator(_store)
            .LoadPerfisAsync(includeGraph: true, cancellationToken);
        return profiles.FirstOrDefault(profile => profile.Id == id);
    }

    public async Task<IReadOnlyCollection<PerfilMapeamento>> GetByDocumentoIdAsync(
        int documentoId,
        CancellationToken cancellationToken = default)
    {
        var profiles = await new SapDataHydrator(_store)
            .LoadPerfisAsync(includeGraph: true, cancellationToken);
        return profiles
            .Where(profile => profile.FK_IdDocumento == documentoId)
            .OrderByDescending(profile => profile.IsPadraoGlobal)
            .ThenBy(profile => profile.Nome)
            .ToList();
    }

    public async Task<IReadOnlyCollection<DocumentoColecao>> GetColecoesDoDocumentoAsync(
        int documentoId,
        CancellationToken cancellationToken = default)
    {
        var links = await new SapDataHydrator(_store)
            .LoadDocumentoColecoesAsync(cancellationToken);
        return links
            .Where(link => link.FK_IdDocumento == documentoId)
            .ToList();
    }

    public async Task<Mapeamento?> GetMapeamentoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Mapeamento,
            id,
            cancellationToken);
        return record is null ? null : SapEntityMapper.ToMapeamento(record);
    }

    public async Task<Documento?> GetDocumentoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Documento,
            id,
            cancellationToken);
        return record is null ? null : SapEntityMapper.ToDocumento(record);
    }

    public Task AddAsync(
        PerfilMapeamento perfil,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingAdds.Add(perfil);
        return Task.CompletedTask;
    }

    public void Remove(PerfilMapeamento perfil)
    {
        if (perfil.Id > 0)
        {
            _pendingDeletes.Add(perfil.Id);
        }
    }

    public async Task RemoveWithOrphanMappingsAsync(
        PerfilMapeamento perfil,
        CancellationToken cancellationToken = default)
    {
        var customMappingIds = perfil.Itens
            .Where(item =>
                item.Mapeamento is not null &&
                !item.Mapeamento.IsPadraoGlobal)
            .Select(item => item.FK_IdMapeamento)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        await DeleteProfileAsync(perfil.Id, cancellationToken);

        foreach (var mappingId in customMappingIds)
        {
            var remainingReferences = await _store.QueryAsync(
                SapUdtSchema.PerfilMapeamentoItem,
                filter: SapOData.Eq("MapeamentoId", mappingId),
                top: 1,
                select: "Code",
                cancellationToken: cancellationToken);
            if (remainingReferences.Count > 0)
            {
                continue;
            }

            var mappingRecord = await _store.GetByIdAsync(
                SapUdtSchema.Mapeamento,
                mappingId,
                cancellationToken);
            if (mappingRecord is null)
            {
                continue;
            }

            var mapping = SapEntityMapper.ToMapeamento(mappingRecord);
            if (mapping.IsPadraoGlobal)
            {
                continue;
            }

            var fieldRows = await _store.QueryAsync(
                SapUdtSchema.MapeamentoCampo,
                filter: SapOData.Eq("MapeamentoId", mappingId),
                cancellationToken: cancellationToken);
            foreach (var field in fieldRows)
            {
                await _store.DeleteAsync(
                    SapUdtSchema.MapeamentoCampo,
                    field.Id,
                    cancellationToken);
            }

            await _store.DeleteAsync(
                SapUdtSchema.Mapeamento,
                mappingId,
                cancellationToken);
        }

        _tracked.Remove(perfil.Id);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var profileId in _pendingDeletes)
        {
            await DeleteProfileAsync(profileId, cancellationToken);
            _tracked.Remove(profileId);
        }

        _pendingDeletes.Clear();

        var insertedIds = new HashSet<int>();
        foreach (var profile in _pendingAdds)
        {
            await PersistNewProfileAsync(profile, cancellationToken);
            insertedIds.Add(profile.Id);
            _tracked[profile.Id] = new TrackedProfile(
                profile,
                profile.Itens.Select(item => item.Id).ToHashSet());
        }

        _pendingAdds.Clear();

        foreach (var tracked in _tracked.Values.Where(
                     value => !insertedIds.Contains(value.Profile.Id)))
        {
            await UpdateProfileAsync(tracked, cancellationToken);
        }
    }

    private async Task PersistNewProfileAsync(
        PerfilMapeamento profile,
        CancellationToken cancellationToken)
    {
        await PersistNewMappingsAsync(profile.Itens, cancellationToken);

        profile.Id = await _store.AddAsync(
            SapUdtSchema.PerfilMapeamento,
            SapEntityMapper.Fields(profile),
            cancellationToken: cancellationToken);

        await PersistItemsAsync(profile, profile.Itens, cancellationToken);
    }

    private async Task UpdateProfileAsync(
        TrackedProfile tracked,
        CancellationToken cancellationToken)
    {
        var profile = tracked.Profile;
        await _store.UpdateAsync(
            SapUdtSchema.PerfilMapeamento,
            profile.Id,
            SapEntityMapper.Fields(profile),
            cancellationToken);

        await PersistNewMappingsAsync(profile.Itens, cancellationToken);

        var desiredExistingIds = profile.Itens
            .Where(item => item.Id > 0)
            .Select(item => item.Id)
            .ToHashSet();
        foreach (var removedId in tracked.OriginalItemIds.Where(
                     id => !desiredExistingIds.Contains(id)))
        {
            await _store.DeleteAsync(
                SapUdtSchema.PerfilMapeamentoItem,
                removedId,
                cancellationToken);
        }

        await PersistItemsAsync(profile, profile.Itens, cancellationToken);
        tracked.OriginalItemIds.Clear();
        tracked.OriginalItemIds.UnionWith(profile.Itens.Select(item => item.Id));
    }

    private async Task PersistNewMappingsAsync(
        IEnumerable<PerfilMapeamentoItem> items,
        CancellationToken cancellationToken)
    {
        var persisted = new HashSet<Mapeamento>(ReferenceEqualityComparer.Instance);

        foreach (var item in items)
        {
            if (item.FK_IdMapeamento > 0)
            {
                continue;
            }

            var mapping = item.Mapeamento;
            if (mapping is null)
            {
                throw new InvalidOperationException(
                    "Item de perfil sem mapeamento persistido ou navegacao de mapeamento.");
            }

            if (mapping.Id <= 0 && persisted.Add(mapping))
            {
                mapping.Id = await _store.AddAsync(
                    SapUdtSchema.Mapeamento,
                    SapEntityMapper.Fields(mapping),
                    cancellationToken: cancellationToken);

                foreach (var field in mapping.Campos)
                {
                    field.FK_IdMapeamento = mapping.Id;
                    field.Mapeamento = mapping;
                    field.Id = await _store.AddAsync(
                        SapUdtSchema.MapeamentoCampo,
                        SapEntityMapper.Fields(field),
                        cancellationToken: cancellationToken);
                }
            }

            item.FK_IdMapeamento = mapping.Id;
        }
    }

    private async Task PersistItemsAsync(
        PerfilMapeamento profile,
        IEnumerable<PerfilMapeamentoItem> items,
        CancellationToken cancellationToken)
    {
        var itemList = items.ToList();
        var completed = new HashSet<PerfilMapeamentoItem>(
            ReferenceEqualityComparer.Instance);
        var visiting = new HashSet<PerfilMapeamentoItem>(
            ReferenceEqualityComparer.Instance);

        async Task PersistAsync(PerfilMapeamentoItem item)
        {
            if (completed.Contains(item))
            {
                return;
            }

            if (!visiting.Add(item))
            {
                throw new InvalidOperationException(
                    "A hierarquia do perfil de mapeamento possui um ciclo.");
            }

            if (item.ItemPai is not null)
            {
                await PersistAsync(item.ItemPai);
                item.FK_IdPerfilMapeamentoItemPai = item.ItemPai.Id;
            }

            item.FK_IdPerfilMapeamento = profile.Id;
            item.PerfilMapeamento = profile;

            if (item.Id > 0)
            {
                await _store.UpdateAsync(
                    SapUdtSchema.PerfilMapeamentoItem,
                    item.Id,
                    SapEntityMapper.Fields(item),
                    cancellationToken);
            }
            else
            {
                item.Id = await _store.AddAsync(
                    SapUdtSchema.PerfilMapeamentoItem,
                    SapEntityMapper.Fields(item),
                    cancellationToken: cancellationToken);
            }

            visiting.Remove(item);
            completed.Add(item);
        }

        foreach (var item in itemList)
        {
            await PersistAsync(item);
        }
    }

    private async Task DeleteProfileAsync(
        int profileId,
        CancellationToken cancellationToken)
    {
        var itemRows = await _store.QueryAsync(
            SapUdtSchema.PerfilMapeamentoItem,
            filter: SapOData.Eq("PerfilId", profileId),
            cancellationToken: cancellationToken);

        foreach (var item in itemRows)
        {
            await _store.DeleteAsync(
                SapUdtSchema.PerfilMapeamentoItem,
                item.Id,
                cancellationToken);
        }

        await _store.DeleteAsync(
            SapUdtSchema.PerfilMapeamento,
            profileId,
            cancellationToken);
    }

    private sealed record TrackedProfile(
        PerfilMapeamento Profile,
        HashSet<int> OriginalItemIds);
}
