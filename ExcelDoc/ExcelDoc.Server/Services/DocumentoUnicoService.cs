using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class DocumentoUnicoService : IDocumentoUnicoService
    {
        public string BuildIdDocumentoUnico(
            Processamento processamento,
            ExcelDocumentGroup group,
            IDictionary<string, object?> payload)
        {
            var signature = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["EmpresaId"] = processamento.FK_IdEmpresa,
                ["DocumentoId"] = processamento.FK_IdDocumento,
                ["PerfilMapeamentoId"] = processamento.FK_IdPerfilMapeamento,
                ["IdExcel"] = group.IdExcel,
                ["QuantidadeLinhas"] = group.Rows.Count,
                ["Payload"] = payload
            };

            var canonicalJson = SerializeCanonical(signature);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));

            return $"v1:{Convert.ToHexString(hash).ToLowerInvariant()}";
        }

        private static string SerializeCanonical(object? value)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteCanonicalValue(writer, value);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteCanonicalValue(Utf8JsonWriter writer, object? value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case string stringValue:
                    writer.WriteStringValue(stringValue.Trim());
                    return;
                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    return;
                case int intValue:
                    writer.WriteNumberValue(intValue);
                    return;
                case long longValue:
                    writer.WriteNumberValue(longValue);
                    return;
                case decimal decimalValue:
                    writer.WriteRawValue(decimalValue.ToString(CultureInfo.InvariantCulture));
                    return;
                case double doubleValue:
                    writer.WriteNumberValue(doubleValue);
                    return;
                case float floatValue:
                    writer.WriteNumberValue(floatValue);
                    return;
                case DateTime dateTimeValue:
                    writer.WriteStringValue(dateTimeValue.ToString("O", CultureInfo.InvariantCulture));
                    return;
                case IDictionary<string, object?> dictionary:
                    WriteDictionary(writer, dictionary);
                    return;
                case IEnumerable enumerable when value is not string:
                    writer.WriteStartArray();
                    foreach (var item in enumerable)
                    {
                        WriteCanonicalValue(writer, item);
                    }
                    writer.WriteEndArray();
                    return;
                default:
                    JsonSerializer.Serialize(writer, value, value.GetType());
                    return;
            }
        }

        private static void WriteDictionary(Utf8JsonWriter writer, IDictionary<string, object?> dictionary)
        {
            writer.WriteStartObject();

            foreach (var pair in dictionary.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(pair.Key);
                WriteCanonicalValue(writer, pair.Value);
            }

            writer.WriteEndObject();
        }
    }
}
