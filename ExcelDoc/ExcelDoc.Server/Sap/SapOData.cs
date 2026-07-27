using System.Globalization;

namespace ExcelDoc.Server.Sap;

public static class SapOData
{
    public static string Field(string alias) => SapUdtSchema.Field(alias);

    public static string Eq(string alias, int value) => $"{Field(alias)} eq {value}";

    public static string Eq(string alias, int? value) =>
        value.HasValue ? Eq(alias, value.Value) : $"{Field(alias)} eq null";

    public static string Eq(string alias, string value) =>
        $"{Field(alias)} eq {String(value)}";

    public static string Ne(string alias, int value) => $"{Field(alias)} ne {value}";

    public static string Ne(string alias, string value) =>
        $"{Field(alias)} ne {String(value)}";

    public static string IsNotNull(string alias) => $"{Field(alias)} ne null";

    public static string String(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    public static string And(params string?[] filters) =>
        Join("and", filters);

    public static string Or(params string?[] filters) =>
        Join("or", filters);

    public static string In(string alias, IEnumerable<int> values)
    {
        var comparisons = values
            .Distinct()
            .Select(value => Eq(alias, value))
            .ToArray();

        return comparisons.Length == 0 ? "false" : Or(comparisons);
    }

    public static string DateTimeValue(DateTime value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string Join(string operation, IEnumerable<string?> filters)
    {
        var values = filters
            .Where(filter => !string.IsNullOrWhiteSpace(filter))
            .Select(filter => $"({filter})")
            .ToArray();

        return values.Length switch
        {
            0 => string.Empty,
            1 => values[0],
            _ => string.Join($" {operation} ", values)
        };
    }
}
