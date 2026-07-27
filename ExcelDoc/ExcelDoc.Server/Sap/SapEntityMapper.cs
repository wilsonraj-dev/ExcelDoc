using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Sap;

internal static class SapEntityMapper
{
    public static Documento ToDocumento(SapUdtRecord record) => new()
    {
        Id = record.Id,
        NomeDocumento = record.GetString("NomeDocumento") ?? string.Empty,
        Endpoint = record.GetString("Endpoint") ?? string.Empty
    };

    public static IReadOnlyDictionary<string, object?> Fields(Documento model) =>
        new Dictionary<string, object?>
        {
            ["NomeDocumento"] = model.NomeDocumento,
            ["Endpoint"] = model.Endpoint
        };

    public static Colecao ToColecao(SapUdtRecord record) => new()
    {
        Id = record.Id,
        NomeColecao = record.GetString("NomeColecao") ?? string.Empty,
        Descricao = record.GetString("Descricao"),
        TipoColecao = record.GetEnum<TipoColecao>("TipoColecao"),
        IsPadrao = record.GetBool("IsPadrao")
    };

    public static IReadOnlyDictionary<string, object?> Fields(Colecao model) =>
        new Dictionary<string, object?>
        {
            ["NomeColecao"] = model.NomeColecao,
            ["Descricao"] = model.Descricao,
            ["TipoColecao"] = (int)model.TipoColecao,
            ["IsPadrao"] = SapBool(model.IsPadrao)
        };

    public static DocumentoColecao ToDocumentoColecao(SapUdtRecord record) => new()
    {
        Id = record.Id,
        FK_IdDocumento = record.GetInt("DocumentoId"),
        FK_IdColecao = record.GetInt("ColecaoId")
    };

    public static IReadOnlyDictionary<string, object?> Fields(DocumentoColecao model) =>
        new Dictionary<string, object?>
        {
            ["DocumentoId"] = model.FK_IdDocumento,
            ["ColecaoId"] = model.FK_IdColecao
        };

    public static Mapeamento ToMapeamento(SapUdtRecord record) => new()
    {
        Id = record.Id,
        Nome = record.GetString("Nome") ?? string.Empty,
        FK_IdColecao = record.GetInt("ColecaoId"),
        IsPadrao = record.GetBool("IsPadrao"),
        DataCriacao = record.GetDateTime("DataCriacao")
    };

    public static IReadOnlyDictionary<string, object?> Fields(Mapeamento model) =>
        new Dictionary<string, object?>
        {
            ["Nome"] = model.Nome,
            ["ColecaoId"] = model.FK_IdColecao,
            ["IsPadrao"] = SapBool(model.IsPadrao),
            ["DataCriacao"] = SapDate(model.DataCriacao)
        };

    public static MapeamentoCampo ToMapeamentoCampo(SapUdtRecord record) => new()
    {
        Id = record.Id,
        IndiceColuna = record.GetInt("IndiceColuna"),
        NomeCampo = record.GetString("NomeCampo") ?? string.Empty,
        DescricaoCampo = record.GetString("DescricaoCampo") ?? string.Empty,
        TipoCampo = record.GetEnum<TipoCampo>("TipoCampo"),
        Formato = record.GetString("Formato"),
        Ativo = record.GetBool("Ativo"),
        FK_IdMapeamento = record.GetInt("MapeamentoId")
    };

    public static IReadOnlyDictionary<string, object?> Fields(MapeamentoCampo model) =>
        new Dictionary<string, object?>
        {
            ["IndiceColuna"] = model.IndiceColuna,
            ["NomeCampo"] = model.NomeCampo,
            ["DescricaoCampo"] = model.DescricaoCampo,
            ["TipoCampo"] = (int)model.TipoCampo,
            ["Formato"] = model.Formato,
            ["Ativo"] = SapBool(model.Ativo),
            ["MapeamentoId"] = model.FK_IdMapeamento
        };

    public static PerfilMapeamento ToPerfilMapeamento(SapUdtRecord record) => new()
    {
        Id = record.Id,
        Nome = record.GetString("Nome") ?? string.Empty,
        FK_IdDocumento = record.GetInt("DocumentoId"),
        IsPadrao = record.GetBool("IsPadrao"),
        DataCriacao = record.GetDateTime("DataCriacao")
    };

    public static IReadOnlyDictionary<string, object?> Fields(PerfilMapeamento model) =>
        new Dictionary<string, object?>
        {
            ["Nome"] = model.Nome,
            ["DocumentoId"] = model.FK_IdDocumento,
            ["IsPadrao"] = SapBool(model.IsPadrao),
            ["DataCriacao"] = SapDate(model.DataCriacao)
        };

    public static PerfilMapeamentoItem ToPerfilMapeamentoItem(SapUdtRecord record) => new()
    {
        Id = record.Id,
        FK_IdPerfilMapeamento = record.GetInt("PerfilId"),
        FK_IdColecao = record.GetInt("ColecaoId"),
        FK_IdMapeamento = record.GetInt("MapeamentoId"),
        FK_IdPerfilMapeamentoItemPai = record.GetNullableInt("ItemPaiId")
    };

    public static IReadOnlyDictionary<string, object?> Fields(PerfilMapeamentoItem model) =>
        new Dictionary<string, object?>
        {
            ["PerfilId"] = model.FK_IdPerfilMapeamento,
            ["ColecaoId"] = model.FK_IdColecao,
            ["MapeamentoId"] = model.FK_IdMapeamento,
            ["ItemPaiId"] = model.FK_IdPerfilMapeamentoItemPai
        };

    public static Processamento ToProcessamento(SapUdtRecord record) => new()
    {
        Id = record.Id,
        UsuarioSAP = record.GetString("UsuarioSAP") ?? string.Empty,
        FK_IdDocumento = record.GetInt("DocumentoId"),
        FK_IdPerfilMapeamento = record.GetNullableInt("PerfilId"),
        NomeArquivo = record.GetString("NomeArquivo") ?? string.Empty,
        DataExecucao = record.GetDateTime("DataExecucao"),
        Status = record.GetEnum<StatusProcessamento>("Status"),
        TotalRegistros = record.GetInt("TotalRegistros"),
        TotalSucesso = record.GetInt("TotalSucesso"),
        TotalErro = record.GetInt("TotalErro"),
        TotalIgnorado = record.GetInt("TotalIgnorado"),
        HashArquivo = record.GetString("HashArquivo") ?? string.Empty
    };

    public static IReadOnlyDictionary<string, object?> Fields(Processamento model) =>
        new Dictionary<string, object?>
        {
            ["UsuarioSAP"] = model.UsuarioSAP,
            ["DocumentoId"] = model.FK_IdDocumento,
            ["PerfilId"] = model.FK_IdPerfilMapeamento,
            ["NomeArquivo"] = model.NomeArquivo,
            ["DataExecucao"] = SapDate(model.DataExecucao),
            ["Status"] = (int)model.Status,
            ["TotalRegistros"] = model.TotalRegistros,
            ["TotalSucesso"] = model.TotalSucesso,
            ["TotalErro"] = model.TotalErro,
            ["TotalIgnorado"] = model.TotalIgnorado,
            ["HashArquivo"] = model.HashArquivo
        };

    public static ProcessamentoItem ToProcessamentoItem(SapUdtRecord record) => new()
    {
        Id = record.Id,
        FK_IdProcessamento = record.GetInt("ProcessamentoId"),
        IdExcel = record.GetNullableInt("IdExcel"),
        IdDocumentoUnico = record.GetString("IdDocumentoUnico"),
        LinhaExcel = record.GetInt("LinhaExcel"),
        JsonEnviado = record.GetString("JsonEnviado") ?? string.Empty,
        JsonRetorno = record.GetString("JsonRetorno"),
        Mensagem = record.GetString("Mensagem"),
        Erro = record.GetString("Erro"),
        Status = record.GetEnum<StatusProcessamentoItem>("Status"),
        DataExecucao = record.GetNullableDateTime("DataExecucao"),
        DataFinalizacao = record.GetNullableDateTime("DataFinalizacao")
    };

    public static IReadOnlyDictionary<string, object?> Fields(ProcessamentoItem model) =>
        new Dictionary<string, object?>
        {
            ["ProcessamentoId"] = model.FK_IdProcessamento,
            ["IdExcel"] = model.IdExcel,
            ["IdDocumentoUnico"] = model.IdDocumentoUnico,
            ["LinhaExcel"] = model.LinhaExcel,
            ["JsonEnviado"] = model.JsonEnviado,
            ["JsonRetorno"] = model.JsonRetorno,
            ["Mensagem"] = model.Mensagem,
            ["Erro"] = model.Erro,
            ["Status"] = (int)model.Status,
            ["DataExecucao"] = SapDate(model.DataExecucao),
            ["DataFinalizacao"] = SapDate(model.DataFinalizacao)
        };

    private static string SapBool(bool value) => value ? "Y" : "N";

    private static string SapDate(DateTime value) => SapOData.DateTimeValue(value);

    private static string? SapDate(DateTime? value) =>
        value.HasValue ? SapDate(value.Value) : null;
}
