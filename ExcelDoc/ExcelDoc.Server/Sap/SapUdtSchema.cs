namespace ExcelDoc.Server.Sap;

public sealed record SapUdfDefinition(
    string Name,
    string Description,
    string Type,
    int? Size = null,
    string? SubType = null);

public sealed record SapUdtKeyDefinition(
    string Name,
    IReadOnlyCollection<string> Columns,
    bool Unique = true);

public sealed record SapUdtDefinition(
    string Name,
    string Description,
    IReadOnlyCollection<SapUdfDefinition> Fields)
{
    // Master Data UDTs are registered as UDOs with the same code. Service Layer
    // exposes the UDO through this code (direct U_<table> access is reserved for
    // UDTs of type bott_NoObject).
    public string Endpoint => Name;
}

public static class SapUdtSchema
{
    public const string Schema = "EXD_SCHEMA";
    public const string Documento = "EXD_DOCUMENTO";
    public const string Colecao = "EXD_COLECAO";
    public const string DocumentoColecao = "EXD_DOCCOL";
    public const string Mapeamento = "EXD_MAPEAMENTO";
    public const string MapeamentoCampo = "EXD_MAPCAMPO";
    public const string PerfilMapeamento = "EXD_PERFILMAP";
    public const string PerfilMapeamentoItem = "EXD_PERFILITEM";
    public const string Processamento = "EXD_PROCESSO";
    public const string ProcessamentoItem = "EXD_PROCITEM";

    public static IReadOnlyCollection<SapUdtDefinition> Tables { get; } =
    [
        Table(Schema, "ExcelDoc - Versao da estrutura",
            Numeric("SchemaVersion", "Versao da estrutura"),
            Numeric("SeedVersion", "Versao dos dados iniciais")),

        Table(Documento, "ExcelDoc - Documentos",
            Alpha("NomeDocumento", "Nome do documento", 150),
            Memo("Endpoint", "Endpoint do documento")),

        Table(Colecao, "ExcelDoc - Colecoes",
            Alpha("NomeColecao", "Nome da colecao", 150),
            Memo("Descricao", "Descricao"),
            Numeric("TipoColecao", "Tipo da colecao"),
            Checkbox("IsPadrao", "Colecao padrao")),

        Table(DocumentoColecao, "ExcelDoc - Documento/Colecao",
            Numeric("DocumentoId", "Documento"),
            Numeric("ColecaoId", "Colecao")),

        Table(Mapeamento, "ExcelDoc - Mapeamentos",
            Alpha("Nome", "Nome", 150),
            Numeric("ColecaoId", "Colecao"),
            Checkbox("IsPadrao", "Mapeamento padrao"),
            Date("DataCriacao", "Data de criacao")),

        Table(MapeamentoCampo, "ExcelDoc - Campos",
            Numeric("IndiceColuna", "Indice da coluna"),
            Alpha("NomeCampo", "Nome do campo", 150),
            Memo("DescricaoCampo", "Descricao do campo"),
            Numeric("TipoCampo", "Tipo do campo"),
            Alpha("Formato", "Formato", 50),
            Checkbox("Ativo", "Ativo"),
            Numeric("MapeamentoId", "Mapeamento")),

        Table(PerfilMapeamento, "ExcelDoc - Perfis",
            Alpha("Nome", "Nome", 150),
            Numeric("DocumentoId", "Documento"),
            Checkbox("IsPadrao", "Perfil padrao"),
            Date("DataCriacao", "Data de criacao")),

        Table(PerfilMapeamentoItem, "ExcelDoc - Itens de perfil",
            Numeric("PerfilId", "Perfil"),
            Numeric("ColecaoId", "Colecao"),
            Numeric("MapeamentoId", "Mapeamento"),
            Numeric("ItemPaiId", "Item pai")),

        Table(Processamento, "ExcelDoc - Processamentos",
            Alpha("UsuarioSAP", "Usuario SAP", 100),
            Numeric("DocumentoId", "Documento"),
            Numeric("PerfilId", "Perfil"),
            Memo("NomeArquivo", "Nome do arquivo"),
            Date("DataExecucao", "Data de execucao"),
            Numeric("Status", "Status"),
            Numeric("TotalRegistros", "Total de registros"),
            Numeric("TotalSucesso", "Total de sucessos"),
            Numeric("TotalErro", "Total de erros"),
            Numeric("TotalIgnorado", "Total ignorado"),
            Alpha("HashArquivo", "Hash do arquivo", 200)),

        Table(ProcessamentoItem, "ExcelDoc - Itens processados",
            Numeric("ProcessamentoId", "Processamento"),
            Numeric("IdExcel", "Identificador Excel"),
            Alpha("IdDocumentoUnico", "Identificador do documento", 128),
            Numeric("LinhaExcel", "Linha no Excel"),
            Memo("JsonEnviado", "JSON enviado"),
            Memo("JsonRetorno", "JSON retornado"),
            Memo("Mensagem", "Mensagem"),
            Memo("Erro", "Erro"),
            Numeric("Status", "Status"),
            Date("DataExecucao", "Data de execucao"),
            Date("DataFinalizacao", "Data de finalizacao"))
    ];

    public static SapUdtDefinition GetTable(string tableName)
    {
        return Tables.FirstOrDefault(
                   table => string.Equals(table.Name, tableName, StringComparison.Ordinal))
               ?? throw new ArgumentOutOfRangeException(
                   nameof(tableName),
                   tableName,
                   "Tabela UDT nao registrada no manifesto do ExcelDoc.");
    }

    public static string Endpoint(string tableName) => GetTable(tableName).Endpoint;

    public static IReadOnlyCollection<SapUdtKeyDefinition> GetKeys(string tableName) =>
        tableName switch
        {
            DocumentoColecao =>
            [
                Key("U_DOCCOL", "DocumentoId", "ColecaoId")
            ],
            MapeamentoCampo =>
            [
                Key("U_MAPCOL", "MapeamentoId", "IndiceColuna")
            ],
            PerfilMapeamentoItem =>
            [
                Key("U_PERFCOL", "PerfilId", "ColecaoId")
            ],
            _ => Array.Empty<SapUdtKeyDefinition>()
        };

    public static string Field(string alias) =>
        alias.StartsWith("U_", StringComparison.Ordinal) ? alias : $"U_{alias}";

    private static SapUdtDefinition Table(
        string name,
        string description,
        params SapUdfDefinition[] fields)
    {
        if (name.Length > 19)
        {
            throw new InvalidOperationException(
                $"O nome da tabela SAP '{name}' excede o limite de 19 caracteres.");
        }

        return new SapUdtDefinition(name, description, fields);
    }

    private static SapUdtKeyDefinition Key(
        string name,
        params string[] columns)
    {
        if (name.Length > 10)
        {
            throw new InvalidOperationException(
                $"O nome da chave SAP '{name}' excede o limite de 10 caracteres.");
        }

        return new SapUdtKeyDefinition(name, columns);
    }

    private static SapUdfDefinition Alpha(string name, string description, int size) =>
        new(name, description, "db_Alpha", size, "st_None");

    private static SapUdfDefinition Memo(string name, string description) =>
        new(name, description, "db_Memo", null, "st_None");

    private static SapUdfDefinition Numeric(string name, string description) =>
        new(name, description, "db_Numeric", 11, "st_None");

    private static SapUdfDefinition Checkbox(string name, string description) =>
        new(name, description, "db_Alpha", 1, "st_Checkbox");

    private static SapUdfDefinition Date(string name, string description) =>
        new(name, description, "db_Alpha", 33, "st_None");
}
