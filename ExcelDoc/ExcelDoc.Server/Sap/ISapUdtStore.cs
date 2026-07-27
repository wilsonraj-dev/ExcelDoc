namespace ExcelDoc.Server.Sap;

public interface ISapUdtStore
{
    Task<IReadOnlyList<SapUdtRecord>> QueryAsync(
        string tableName,
        string? filter = null,
        string? orderBy = null,
        int? top = null,
        int? skip = null,
        string? select = null,
        CancellationToken cancellationToken = default);

    Task<SapUdtRecord?> GetByIdAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string tableName,
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<int> AddAsync(
        string tableName,
        IReadOnlyDictionary<string, object?> fields,
        string? name = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        string tableName,
        int id,
        IReadOnlyDictionary<string, object?> fields,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default);
}
