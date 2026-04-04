using System.Globalization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class PayloadBuilderService : IPayloadBuilderService
    {
        public IDictionary<string, object?> BuildPayload(Documento documento, IReadOnlyDictionary<int, string?> rowValues)
        {
            var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var lineCollections = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);

            foreach (var documentoColecao in documento.DocumentoColecoes)
            {
                var colecao = documentoColecao.Colecao;
                if (colecao is null)
                {
                    continue;
                }

                if (colecao.TipoColecao == TipoColecao.Header)
                {
                    foreach (var campo in colecao.MapeamentoCampos.OrderBy(x => x.IndiceColuna))
                    {
                        rowValues.TryGetValue(campo.IndiceColuna, out var rawValue);
                        payload[campo.NomeCampo] = ConvertValue(rawValue, campo);
                    }

                    continue;
                }

                var line = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var campo in colecao.MapeamentoCampos.OrderBy(x => x.IndiceColuna))
                {
                    rowValues.TryGetValue(campo.IndiceColuna, out var rawValue);
                    line[campo.NomeCampo] = ConvertValue(rawValue, campo);
                }

                if (!lineCollections.TryGetValue(colecao.NomeColecao, out var items))
                {
                    items = new List<Dictionary<string, object?>>();
                    lineCollections[colecao.NomeColecao] = items;
                }

                items.Add(line);
            }

            foreach (var lineCollection in lineCollections)
            {
                payload[lineCollection.Key] = lineCollection.Value;
            }

            return payload;
        }

        private static object? ConvertValue(string? rawValue, MapeamentoCampo campo)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var value = rawValue.Trim();

            return campo.TipoCampo switch
            {
                TipoCampo.String => value,
                TipoCampo.Int => int.Parse(value, CultureInfo.InvariantCulture),
                TipoCampo.Double => double.Parse(value, CultureInfo.InvariantCulture),
                TipoCampo.DateTime => ParseDateTime(value, campo.Formato),
                TipoCampo.Boolean => ParseBoolean(value),
                _ => value
            };
        }

        private static string ParseDateTime(string value, string? format)
        {
            DateTime date;

            if (!string.IsNullOrWhiteSpace(format))
            {
                date = DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
            }
            else
            {
                date = DateTime.Parse(value, CultureInfo.InvariantCulture);
            }

            return date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static bool ParseBoolean(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "1" => true,
                "true" => true,
                "sim" => true,
                "s" => true,
                "yes" => true,
                "y" => true,
                "0" => false,
                "false" => false,
                "nao" => false,
                "não" => false,
                "n" => false,
                "no" => false,
                _ => throw new FormatException($"Valor booleano inválido: {value}")
            };
        }
    }
}
