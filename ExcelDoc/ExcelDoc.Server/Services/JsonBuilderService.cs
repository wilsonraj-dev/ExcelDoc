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
                .OrderBy(i => i.Id)
                .ToList();

            var lineItems = perfil.Itens
                .Where(i => i.Colecao.TipoColecao == TipoColecao.Line)
                .OrderBy(i => i.Id)
                .ToList();

            var lineChildrenByParentId = lineItems
                .Where(i => i.FK_IdPerfilMapeamentoItemPai.HasValue)
                .GroupBy(i => i.FK_IdPerfilMapeamentoItemPai!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList());

            var rootLineItems = lineItems
                .Where(i => !i.FK_IdPerfilMapeamentoItemPai.HasValue)
                .ToList();

            var firstRow = groupRows[0];

            foreach (var headerItem in headerItems)
            {
                var headerPayload = _payloadBuilder.BuildPayload(
                    perfil.Documento, headerItem.Mapeamento, firstRow.Values);

                foreach (var kvp in headerPayload)
                {
                    document[kvp.Key] = kvp.Value;
                }
            }

            foreach (var lineItem in rootLineItems)
            {
                document[lineItem.Colecao.NomeColecao] = BuildLineCollection(
                    perfil,
                    lineItem,
                    groupRows,
                    lineChildrenByParentId);
            }

            return document;
        }

        private List<IDictionary<string, object?>> BuildLineCollection(
            PerfilMapeamento perfil,
            PerfilMapeamentoItem lineItem,
            IReadOnlyList<ExcelRowData> rows,
            IReadOnlyDictionary<int, List<PerfilMapeamentoItem>> childrenByParentId)
        {
            var collection = new List<IDictionary<string, object?>>();

            foreach (var row in rows)
            {
                var linePayload = _payloadBuilder.BuildPayload(
                    perfil.Documento, lineItem.Mapeamento, row.Values);

                if (childrenByParentId.TryGetValue(lineItem.Id, out var childItems))
                {
                    foreach (var childItem in childItems)
                    {
                        linePayload[childItem.Colecao.NomeColecao] = BuildLineCollection(
                            perfil,
                            childItem,
                            [row],
                            childrenByParentId);
                    }
                }

                collection.Add(linePayload);
            }

            return collection;
        }
    }
}
