using System.Globalization;
using System.Text.Json;

namespace ExcelDoc.Server.Sap;

public sealed class SapUdtRecord
{
    private readonly JsonElement _value;

    public SapUdtRecord(JsonElement value)
    {
        _value = value.Clone();
    }

    public int Id => ParseInt(GetElement("Code"))
        ?? throw new InvalidOperationException("Registro UDT sem um Code numerico valido.");

    public string? GetString(string alias)
    {
        var value = GetElement(SapUdtSchema.Field(alias));
        if (!value.HasValue ||
            value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.Value.ValueKind == JsonValueKind.String
            ? value.Value.GetString()
            : value.Value.ToString();
    }

    public int GetInt(string alias) =>
        GetNullableInt(alias)
        ?? throw new InvalidOperationException(
            $"O campo '{SapUdtSchema.Field(alias)}' nao possui um inteiro valido.");

    public int? GetNullableInt(string alias) =>
        ParseInt(GetElement(SapUdtSchema.Field(alias)));

    public bool GetBool(string alias)
    {
        var value = GetString(alias);
        return string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "tYES", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               value == "1";
    }

    public DateTime GetDateTime(string alias) =>
        GetNullableDateTime(alias)
        ?? throw new InvalidOperationException(
            $"O campo '{SapUdtSchema.Field(alias)}' nao possui uma data valida.");

    public DateTime? GetNullableDateTime(string alias)
    {
        var value = GetString(alias);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }

    public TEnum GetEnum<TEnum>(string alias)
        where TEnum : struct, Enum
    {
        var numeric = GetNullableInt(alias);
        if (numeric.HasValue && Enum.IsDefined(typeof(TEnum), numeric.Value))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numeric.Value);
        }

        var value = GetString(alias);
        if (Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"O campo '{SapUdtSchema.Field(alias)}' nao possui um valor valido para {typeof(TEnum).Name}.");
    }

    private JsonElement? GetElement(string propertyName)
    {
        return _value.TryGetProperty(propertyName, out var value) ? value : null;
    }

    private static int? ParseInt(JsonElement? value)
    {
        if (!value.HasValue ||
            value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.Value.ValueKind == JsonValueKind.Number &&
            value.Value.TryGetInt32(out var numeric))
        {
            return numeric;
        }

        if (value.Value.ValueKind == JsonValueKind.Number &&
            value.Value.TryGetDecimal(out var decimalValue) &&
            decimalValue == decimal.Truncate(decimalValue) &&
            decimalValue is >= int.MinValue and <= int.MaxValue)
        {
            return decimal.ToInt32(decimalValue);
        }

        var text = value.Value.ValueKind == JsonValueKind.String
            ? value.Value.GetString()
            : value.Value.ToString();

        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return decimal.TryParse(
                   text,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out decimalValue) &&
               decimalValue == decimal.Truncate(decimalValue) &&
               decimalValue is >= int.MinValue and <= int.MaxValue
            ? decimal.ToInt32(decimalValue)
            : null;
    }
}
