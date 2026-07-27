using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Repositories;

public sealed class MapeamentoRepository : IMapeamentoRepository
{
    private readonly ISapUdtStore _store;
    private readonly List<Mapeamento> _pendingMappings = [];
    private readonly List<MapeamentoCampo> _pendingFields = [];
    private readonly Dictionary<int, Mapeamento> _trackedMappings = [];
    private readonly Dictionary<int, MapeamentoCampo> _trackedFields = [];
    private readonly HashSet<int> _pendingMappingDeletes = [];
    private readonly HashSet<int> _pendingFieldDeletes = [];

    public MapeamentoRepository(ISapUdtStore store)
    {
        _store = store;
    }

    public async Task<Colecao?> GetColecaoByIdAsync(
        int colecaoId,
        CancellationToken cancellationToken = default)
    {
        var record = await _store.GetByIdAsync(
            SapUdtSchema.Colecao,
            colecaoId,
            cancellationToken);
        return record is null ? null : SapEntityMapper.ToColecao(record);
    }

    public async Task<IReadOnlyCollection<Mapeamento>> GetMapeamentosByColecaoIdAsync(
        int colecaoId,
        CancellationToken cancellationToken = default)
    {
        var mappings = await new SapDataHydrator(_store)
            .LoadMapeamentosAsync(
                includeColecao: false,
                includeCampos: true,
                cancellationToken);

        return mappings
            .Where(mapping => mapping.FK_IdColecao == colecaoId)
            .OrderByDescending(mapping => mapping.IsPadraoGlobal)
            .ThenBy(mapping => mapping.Nome)
            .ToList();
    }

    public async Task<Mapeamento?> GetMapeamentoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mappings = await new SapDataHydrator(_store)
            .LoadMapeamentosAsync(
                includeColecao: true,
                includeCampos: true,
                cancellationToken);
        var mapping = mappings.FirstOrDefault(value => value.Id == id);
        if (mapping is null)
        {
            return null;
        }

        _trackedMappings[mapping.Id] = mapping;
        foreach (var field in mapping.Campos)
        {
            _trackedFields[field.Id] = field;
        }

        return mapping;
    }

    public async Task<MapeamentoCampo?> GetCampoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mappings = await new SapDataHydrator(_store)
            .LoadMapeamentosAsync(
                includeColecao: true,
                includeCampos: true,
                cancellationToken);
        var field = mappings
            .SelectMany(mapping => mapping.Campos)
            .FirstOrDefault(value => value.Id == id);
        if (field is not null)
        {
            _trackedFields[field.Id] = field;
        }

        return field;
    }

    public async Task<IReadOnlyCollection<MapeamentoCampo>> GetCamposByMapeamentoIdAsync(
        int mapeamentoId,
        CancellationToken cancellationToken = default)
    {
        var records = await _store.QueryAsync(
            SapUdtSchema.MapeamentoCampo,
            filter: SapOData.Eq("MapeamentoId", mapeamentoId),
            cancellationToken: cancellationToken);
        return records
            .Select(SapEntityMapper.ToMapeamentoCampo)
            .OrderBy(field => field.IndiceColuna)
            .ToList();
    }

    public async Task<bool> ExistsIndiceNoMapeamentoAsync(
        int mapeamentoId,
        int indiceColuna,
        int? ignoreId = null,
        CancellationToken cancellationToken = default)
    {
        var records = await _store.QueryAsync(
            SapUdtSchema.MapeamentoCampo,
            filter: SapOData.And(
                SapOData.Eq("MapeamentoId", mapeamentoId),
                SapOData.Eq("IndiceColuna", indiceColuna)),
            cancellationToken: cancellationToken);
        return records.Any(record => !ignoreId.HasValue || record.Id != ignoreId.Value);
    }

    public Task AddMapeamentoAsync(
        Mapeamento mapeamento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingMappings.Add(mapeamento);
        return Task.CompletedTask;
    }

    public Task AddCampoAsync(
        MapeamentoCampo campo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingFields.Add(campo);
        return Task.CompletedTask;
    }

    public void RemoveMapeamento(Mapeamento mapeamento)
    {
        if (mapeamento.Id > 0)
        {
            _pendingMappingDeletes.Add(mapeamento.Id);
        }
    }

    public void RemoveCampo(MapeamentoCampo campo)
    {
        if (campo.Id > 0)
        {
            _pendingFieldDeletes.Add(campo.Id);
        }
    }

    public async Task ReplaceCamposAsync(
        Mapeamento mapeamento,
        IReadOnlyCollection<MapeamentoCampo> campos,
        CancellationToken cancellationToken = default)
    {
        var currentRows = await _store.QueryAsync(
            SapUdtSchema.MapeamentoCampo,
            filter: SapOData.Eq("MapeamentoId", mapeamento.Id),
            cancellationToken: cancellationToken);
        var currentFields = currentRows
            .Select(SapEntityMapper.ToMapeamentoCampo)
            .ToDictionary(field => field.Id);
        var desiredIds = campos
            .Where(field => field.Id > 0)
            .Select(field => field.Id)
            .ToHashSet();

        foreach (var removed in currentFields.Values.Where(
                     field => !desiredIds.Contains(field.Id)))
        {
            await _store.DeleteAsync(
                SapUdtSchema.MapeamentoCampo,
                removed.Id,
                cancellationToken);
            _trackedFields.Remove(removed.Id);
        }

        foreach (var desired in campos)
        {
            desired.FK_IdMapeamento = mapeamento.Id;
            desired.Mapeamento = mapeamento;

            if (desired.Id > 0)
            {
                await _store.UpdateAsync(
                    SapUdtSchema.MapeamentoCampo,
                    desired.Id,
                    SapEntityMapper.Fields(desired),
                    cancellationToken);
            }
            else
            {
                desired.Id = await _store.AddAsync(
                    SapUdtSchema.MapeamentoCampo,
                    SapEntityMapper.Fields(desired),
                    cancellationToken: cancellationToken);
            }

            _trackedFields[desired.Id] = desired;
        }

        mapeamento.Campos = campos
            .OrderBy(field => field.IndiceColuna)
            .ToList();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var fieldId in _pendingFieldDeletes)
        {
            await _store.DeleteAsync(
                SapUdtSchema.MapeamentoCampo,
                fieldId,
                cancellationToken);
            _trackedFields.Remove(fieldId);
        }

        _pendingFieldDeletes.Clear();

        foreach (var mappingId in _pendingMappingDeletes)
        {
            var references = await _store.QueryAsync(
                SapUdtSchema.PerfilMapeamentoItem,
                filter: SapOData.Eq("MapeamentoId", mappingId),
                top: 1,
                select: "Code",
                cancellationToken: cancellationToken);
            if (references.Count > 0)
            {
                throw new InvalidOperationException(
                    "O mapeamento possui perfis vinculados.");
            }

            var fields = await _store.QueryAsync(
                SapUdtSchema.MapeamentoCampo,
                filter: SapOData.Eq("MapeamentoId", mappingId),
                cancellationToken: cancellationToken);
            foreach (var field in fields)
            {
                await _store.DeleteAsync(
                    SapUdtSchema.MapeamentoCampo,
                    field.Id,
                    cancellationToken);
                _trackedFields.Remove(field.Id);
            }

            await _store.DeleteAsync(
                SapUdtSchema.Mapeamento,
                mappingId,
                cancellationToken);
            _trackedMappings.Remove(mappingId);
        }

        _pendingMappingDeletes.Clear();

        var insertedMappingIds = new HashSet<int>();
        var insertedFieldIds = new HashSet<int>();
        foreach (var mapping in _pendingMappings)
        {
            mapping.Id = await _store.AddAsync(
                SapUdtSchema.Mapeamento,
                SapEntityMapper.Fields(mapping),
                cancellationToken: cancellationToken);
            insertedMappingIds.Add(mapping.Id);
            _trackedMappings[mapping.Id] = mapping;

            foreach (var field in mapping.Campos)
            {
                field.FK_IdMapeamento = mapping.Id;
                field.Mapeamento = mapping;
                field.Id = await _store.AddAsync(
                    SapUdtSchema.MapeamentoCampo,
                    SapEntityMapper.Fields(field),
                    cancellationToken: cancellationToken);
                insertedFieldIds.Add(field.Id);
                _trackedFields[field.Id] = field;
            }
        }

        _pendingMappings.Clear();

        foreach (var field in _pendingFields)
        {
            field.Id = await _store.AddAsync(
                SapUdtSchema.MapeamentoCampo,
                SapEntityMapper.Fields(field),
                cancellationToken: cancellationToken);
            insertedFieldIds.Add(field.Id);
            _trackedFields[field.Id] = field;
        }

        _pendingFields.Clear();

        foreach (var mapping in _trackedMappings.Values.Where(
                     value => !insertedMappingIds.Contains(value.Id)))
        {
            await _store.UpdateAsync(
                SapUdtSchema.Mapeamento,
                mapping.Id,
                SapEntityMapper.Fields(mapping),
                cancellationToken);
        }

        foreach (var field in _trackedFields.Values.Where(
                     value => !insertedFieldIds.Contains(value.Id)))
        {
            await _store.UpdateAsync(
                SapUdtSchema.MapeamentoCampo,
                field.Id,
                SapEntityMapper.Fields(field),
                cancellationToken);
        }
    }
}
