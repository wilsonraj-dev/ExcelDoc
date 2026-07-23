using ExcelDoc.Server.Background;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services;

namespace ExcelDoc.Server.Tests;

public sealed class JsonBuilderServiceTests
{
    private readonly JsonBuilderService _service = new(
        new PayloadBuilderService(new StubMessageService()));

    [Fact]
    public void BuildDocumentPayload_OmitsEmptyOptionalRootAndChildCollections()
    {
        var document = new Documento { Id = 1 };
        var header = CreateItem(1, "Header", TipoColecao.Header, CreateMapping("DocDate", 1));
        var lines = CreateItem(2, "DocumentLines", TipoColecao.Line, CreateMapping("ItemCode", 2));
        var emptyChild = CreateItem(3, "BatchNumbers", TipoColecao.Line, CreateMapping("BatchNumber", 3), 2);
        var emptyRoot = CreateItem(4, "DocumentInstallments", TipoColecao.Line, CreateMapping("DueDate", 4));
        var profile = CreateProfile(document, header, lines, emptyChild, emptyRoot);

        var payload = _service.BuildDocumentPayload(
            profile,
            [CreateRow((1, "2026-07-17"), (2, "A0001"), (3, "   "), (4, null))]);

        var documentLines = Assert.IsType<List<IDictionary<string, object?>>>(payload["DocumentLines"]);
        var documentLine = Assert.Single(documentLines);

        Assert.Equal("A0001", documentLine["ItemCode"]);
        Assert.False(documentLine.ContainsKey("BatchNumbers"));
        Assert.False(payload.ContainsKey("DocumentInstallments"));
    }

    [Fact]
    public void BuildDocumentPayload_TreatsZeroAndFalseAsMeaningfulIncludingNestedChild()
    {
        var document = new Documento { Id = 1 };
        var lines = CreateItem(1, "DocumentLines", TipoColecao.Line, CreateMapping("ItemCode", 1));
        var child = CreateItem(
            2,
            "BatchNumbers",
            TipoColecao.Line,
            CreateMapping("IsManual", 2, TipoCampo.Boolean),
            1);
        var installments = CreateItem(
            3,
            "DocumentInstallments",
            TipoColecao.Line,
            CreateMapping("InstallmentId", 3, TipoCampo.Int));
        var profile = CreateProfile(document, lines, child, installments);

        var payload = _service.BuildDocumentPayload(
            profile,
            [CreateRow((1, " "), (2, "false"), (3, "0"))]);

        var documentLine = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(payload["DocumentLines"]));
        var batch = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(documentLine["BatchNumbers"]));
        var installment = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(payload["DocumentInstallments"]));

        Assert.Equal(false, batch["IsManual"]);
        Assert.Equal(0, installment["InstallmentId"]);
    }

    [Fact]
    public void BuildDocumentPayload_PreservesNestedCollectionHierarchy()
    {
        var document = new Documento { Id = 1 };
        var header = CreateItem(1, "Header", TipoColecao.Header, CreateMapping("CardCode", 1));
        var lines = CreateItem(10, "DocumentLines", TipoColecao.Line, CreateMapping("ItemCode", 2));
        var batches = CreateItem(11, "BatchNumbers", TipoColecao.Line, CreateMapping("BatchNumber", 3), 10);
        var allocations = CreateItem(
            12,
            "DocumentLinesBinAllocations",
            TipoColecao.Line,
            CreateMapping("BinAbsEntry", 4, TipoCampo.Int),
            11);
        var profile = CreateProfile(document, header, lines, batches, allocations);

        var payload = _service.BuildDocumentPayload(
            profile,
            [CreateRow((1, "C20000"), (2, "A0001"), (3, "LOTE-01"), (4, "42"))]);

        var documentLine = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(payload["DocumentLines"]));
        var batch = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(documentLine["BatchNumbers"]));
        var allocation = Assert.Single(
            Assert.IsType<List<IDictionary<string, object?>>>(batch["DocumentLinesBinAllocations"]));

        Assert.Equal("A0001", documentLine["ItemCode"]);
        Assert.Equal("LOTE-01", batch["BatchNumber"]);
        Assert.Equal(42, allocation["BinAbsEntry"]);
    }

    private static PerfilMapeamento CreateProfile(Documento document, params PerfilMapeamentoItem[] items) =>
        new()
        {
            Documento = document,
            FK_IdDocumento = document.Id,
            Itens = items.ToList()
        };

    private static PerfilMapeamentoItem CreateItem(
        int id,
        string collectionName,
        TipoColecao collectionType,
        Mapeamento mapping,
        int? parentId = null) =>
        new()
        {
            Id = id,
            FK_IdPerfilMapeamentoItemPai = parentId,
            Colecao = new Colecao
            {
                Id = id,
                NomeColecao = collectionName,
                TipoColecao = collectionType
            },
            Mapeamento = mapping
        };

    private static Mapeamento CreateMapping(
        string fieldName,
        int columnIndex,
        TipoCampo fieldType = TipoCampo.String) =>
        new()
        {
            Campos =
            [
                new MapeamentoCampo
                {
                    NomeCampo = fieldName,
                    IndiceColuna = columnIndex,
                    TipoCampo = fieldType,
                    Ativo = true
                }
            ]
        };

    private static ExcelRowData CreateRow(params (int ColumnIndex, string? Value)[] values) =>
        new()
        {
            RowNumber = 2,
            Values = values.ToDictionary(item => item.ColumnIndex, item => item.Value)
        };
}
