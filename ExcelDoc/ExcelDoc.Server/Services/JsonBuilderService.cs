using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class JsonBuilderService : IJsonBuilderService
    {
        private readonly IPayloadBuilderService _payloadBuilder;

        public JsonBuilderService(IPayloadBuilderService payloadBuilder)
        {
            _payloadBuilder = payloadBuilder;
        }

        public IDictionary<string, object?> BuildDocumentPayload(
            PerfilMapeamento perfil,
            IReadOnlyList<ExcelRowData> groupRows)
        {
            var document = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var headerItems = perfil.Itens
                .Where(i => i.Colecao.TipoColecao == TipoColecao.Header)
                .ToList();

            var lineItems = perfil.Itens
                .Where(i => i.Colecao.TipoColecao == TipoColecao.Line)
                .ToList();

            var firstRow = groupRows[0];

            foreach (var headerItem in headerItems)
            {
                var mapeamento = headerItem.Mapeamento;
                var headerPayload = _payloadBuilder.BuildPayload(
                    perfil.Documento, mapeamento, firstRow.Values);

                foreach (var kvp in headerPayload)
                {
                    document[kvp.Key] = kvp.Value;
                }
            }

            foreach (var lineItem in lineItems)
            {
                var mapeamento = lineItem.Mapeamento;
                var collectionName = lineItem.Colecao.NomeColecao;
                var lines = new List<IDictionary<string, object?>>();

                foreach (var row in groupRows)
                {
                    var linePayload = _payloadBuilder.BuildPayload(
                        perfil.Documento, mapeamento, row.Values);
                    lines.Add(linePayload);
                }

                document[collectionName] = lines;
            }

            return document;
        }


    }
}
