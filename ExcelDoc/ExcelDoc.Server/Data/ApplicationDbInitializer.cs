using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Data
{
    public static class ApplicationDbInitializer
    {
        private const string EmpresaNomePadrao = "B2Finance";
        private const string UsuarioNomePadrao = "Wilson";
        private const string UsuarioSenhaPadrao = "B1@Admin";
        private const string UsuarioEmailPadrao = "wilson.assis.junior@gmail.com";
        private const string MapeamentoCabecalhoNomePadrao = "Mapeamento Padrão - Cabeçalho";
        private const string MapeamentoDocumentLinesNomePadrao = "Mapeamento Padrão - DocumentLines";
        private const string MapeamentoParcelasNomePadrao = "Mapeamento Padrão - DocumentInstallments";
        private const string PerfilDocumentosDeMarketingPadrao = "Documentos de Marketing";

        private static readonly string[] MarketingDocumentEndpoints =
        {
            "PurchaseInvoices",
            "Invoices",
            "Orders",
            "PurchaseOrders",
            "PurchaseDownPayments",
            "DownPayments",
            "PurchaseQuotations",
            "PurchaseRequests",
            "PurchaseDeliveryNotes",
            "PurchaseCreditNotes",
            "CreditNotes",
            "PurchaseReturns",
            "GoodsReturnRequest",
            "Quotations",
            "DeliveryNotes",
            "Returns",
            "ReturnRequest"
        };

        public static async Task ApplyMigrationsAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            await ExecuteWithDatabaseLockAsync(
                dbContext,
                "ExcelDoc:ApplyMigrations",
                async () =>
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    await EnsurePerfilMapeamentoItemPaiSchemaAsync(dbContext, logger, cancellationToken);
                },
                logger,
                cancellationToken);
        }

        public static async Task InstallSapDefaultsAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            await ExecuteWithDatabaseLockAsync(
                dbContext,
                "ExcelDoc:InstallSapDefaults",
                async () =>
                {
                    var documentos = await EnsureDocumentosAsync(dbContext, logger, cancellationToken);
                    var colecoes = await EnsureColecoesAsync(dbContext, logger, cancellationToken);

                    await EnsureDocumentoColecoesAsync(dbContext, documentos, colecoes, logger, cancellationToken);

                    var mapeamentos = await EnsureMapeamentosAsync(dbContext, colecoes, logger, cancellationToken);
                    await EnsurePerfilMapeamentoAsync(dbContext, documentos, colecoes, mapeamentos, logger, cancellationToken);
                },
                logger,
                cancellationToken);
        }

        private static async Task ExecuteWithDatabaseLockAsync(
            ExcelDocDbContext dbContext,
            string lockName,
            Func<Task> action,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var connection = dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;
            var lockAcquired = false;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using (var acquireCommand = connection.CreateCommand())
                {
                    acquireCommand.CommandText = "SELECT GET_LOCK(@lockName, @timeoutSeconds);";

                    var lockNameParameter = acquireCommand.CreateParameter();
                    lockNameParameter.ParameterName = "@lockName";
                    lockNameParameter.Value = lockName;
                    acquireCommand.Parameters.Add(lockNameParameter);

                    var timeoutParameter = acquireCommand.CreateParameter();
                    timeoutParameter.ParameterName = "@timeoutSeconds";
                    timeoutParameter.Value = 60;
                    acquireCommand.Parameters.Add(timeoutParameter);

                    var result = await acquireCommand.ExecuteScalarAsync(cancellationToken);
                    lockAcquired = result is not null && result != DBNull.Value && Convert.ToInt32(result) == 1;
                }

                if (!lockAcquired)
                {
                    throw new TimeoutException($"Não foi possível obter o lock de inicialização '{lockName}'.");
                }

                logger.LogInformation("Lock de inicialização {LockName} obtido.", lockName);
                await action();
            }
            finally
            {
                if (lockAcquired)
                {
                    await using var releaseCommand = connection.CreateCommand();
                    releaseCommand.CommandText = "SELECT RELEASE_LOCK(@lockName);";

                    var lockNameParameter = releaseCommand.CreateParameter();
                    lockNameParameter.ParameterName = "@lockName";
                    lockNameParameter.Value = lockName;
                    releaseCommand.Parameters.Add(lockNameParameter);

                    await releaseCommand.ExecuteScalarAsync(CancellationToken.None);
                    logger.LogInformation("Lock de inicialização {LockName} liberado.", lockName);
                }

                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public static async Task InstallDevelopmentSampleDataAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var passwordHasherService = scopedProvider.GetRequiredService<IPasswordHasherService>();
            var encryptionService = scopedProvider.GetRequiredService<IEncryptionService>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            var empresa = await EnsureEmpresaAsync(dbContext, logger, cancellationToken);
            await EnsureConfiguracaoAsync(dbContext, encryptionService, empresa.Id, logger, cancellationToken);
            await EnsureUsuarioPadraoAsync(dbContext, passwordHasherService, empresa.Id, logger, cancellationToken);
        }

        private static async Task EnsurePerfilMapeamentoItemPaiSchemaAsync(
            ExcelDocDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            const string tableName = "PerfilMapeamentoItem";
            const string columnName = "FK_IdPerfilMapeamentoItemPai";
            const string indexName = "IX_PerfilMapeamentoItem_FK_IdPerfilMapeamentoItemPai";
            const string foreignKeyName = "FK_PerfilMapeamentoItem_PerfilMapeamentoItemPai";

            if (!await ColumnExistsAsync(dbContext, tableName, columnName, cancellationToken))
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` int NULL;",
                    cancellationToken);

                logger.LogWarning(
                    "Coluna {ColumnName} criada em {TableName} porque a migration estava registrada sem alterar o schema.",
                    columnName,
                    tableName);
            }

            if (!await IndexExistsAsync(dbContext, tableName, indexName, cancellationToken))
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"CREATE INDEX `{indexName}` ON `{tableName}` (`{columnName}`);",
                    cancellationToken);
            }

            if (!await ForeignKeyExistsAsync(dbContext, tableName, foreignKeyName, cancellationToken))
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"""
                    ALTER TABLE `{tableName}`
                    ADD CONSTRAINT `{foreignKeyName}`
                    FOREIGN KEY (`{columnName}`) REFERENCES `{tableName}` (`Id`)
                    ON DELETE RESTRICT;
                    """,
                    cancellationToken);
            }
        }

        private static async Task<bool> ColumnExistsAsync(
            ExcelDocDbContext dbContext,
            string tableName,
            string columnName,
            CancellationToken cancellationToken)
        {
            return await SchemaObjectExistsAsync(
                dbContext,
                """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @tableName
                  AND COLUMN_NAME = @objectName;
                """,
                tableName,
                columnName,
                cancellationToken);
        }

        private static async Task<bool> IndexExistsAsync(
            ExcelDocDbContext dbContext,
            string tableName,
            string indexName,
            CancellationToken cancellationToken)
        {
            return await SchemaObjectExistsAsync(
                dbContext,
                """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @tableName
                  AND INDEX_NAME = @objectName;
                """,
                tableName,
                indexName,
                cancellationToken);
        }

        private static async Task<bool> ForeignKeyExistsAsync(
            ExcelDocDbContext dbContext,
            string tableName,
            string foreignKeyName,
            CancellationToken cancellationToken)
        {
            return await SchemaObjectExistsAsync(
                dbContext,
                """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = @tableName
                  AND CONSTRAINT_NAME = @objectName
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY';
                """,
                tableName,
                foreignKeyName,
                cancellationToken);
        }

        private static async Task<bool> SchemaObjectExistsAsync(
            ExcelDocDbContext dbContext,
            string commandText,
            string tableName,
            string objectName,
            CancellationToken cancellationToken)
        {
            var connection = dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = commandText;

                var tableParameter = command.CreateParameter();
                tableParameter.ParameterName = "@tableName";
                tableParameter.Value = tableName;
                command.Parameters.Add(tableParameter);

                var objectParameter = command.CreateParameter();
                objectParameter.ParameterName = "@objectName";
                objectParameter.Value = objectName;
                command.Parameters.Add(objectParameter);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result) > 0;
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static async Task<Empresa> EnsureEmpresaAsync(
            ExcelDocDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var empresa = await dbContext.Empresas
                .FirstOrDefaultAsync(x => x.NomeEmpresa == EmpresaNomePadrao, cancellationToken);

            if (empresa is not null)
            {
                logger.LogInformation("Empresa padrão {EmpresaNome} já existente com Id {EmpresaId}.", empresa.NomeEmpresa, empresa.Id);
                return empresa;
            }

            empresa = new Empresa
            {
                NomeEmpresa = EmpresaNomePadrao
            };

            await dbContext.Empresas.AddAsync(empresa, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Empresa padrão {EmpresaNome} criada com Id {EmpresaId}.", empresa.NomeEmpresa, empresa.Id);

            return empresa;
        }

        private static async Task EnsureConfiguracaoAsync(
            ExcelDocDbContext dbContext,
            IEncryptionService encryptionService,
            int empresaId,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var configuracao = await dbContext.Configuracoes
                .FirstOrDefaultAsync(x => x.FK_IdEmpresa == empresaId, cancellationToken);

            if (configuracao is not null)
            {
                logger.LogInformation("Configuração padrão da empresa {EmpresaId} já existente com Id {ConfiguracaoId}.", empresaId, configuracao.Id);
                return;
            }

            configuracao = new Configuracao
            {
                LinkServiceLayer = "https://b1.ativy.com:50824",
                Database = "SBO_FABRICA_SOFTWARE",
                UsuarioSAP = encryptionService.Encrypt("DEV_03"),
                SenhaSAP = encryptionService.Encrypt("B1Admin@"),
                FK_IdEmpresa = empresaId
            };

            await dbContext.Configuracoes.AddAsync(configuracao, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Configuração padrão criada para a empresa {EmpresaId} com Id {ConfiguracaoId}.", empresaId, configuracao.Id);
        }

        private static async Task<Dictionary<string, Documento>> EnsureDocumentosAsync(ExcelDocDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new DocumentoSeed("Nota Fiscal de Entrada", "PurchaseInvoices"),
                new DocumentoSeed("Nota Fiscal de Saída", "Invoices"),
                new DocumentoSeed("Pedido de Venda", "Orders"),
                new DocumentoSeed("Pedido de Compra", "PurchaseOrders"),
                new DocumentoSeed("Adiantamento de Fornecedor", "PurchaseDownPayments"),
                new DocumentoSeed("Adiantamento de Cliente", "DownPayments"),
                new DocumentoSeed("Oferta de Compra", "PurchaseQuotations"),
                new DocumentoSeed("Solicitação de Compra", "PurchaseRequests"),
                new DocumentoSeed("Recebimento de mercadorias", "PurchaseDeliveryNotes"),
                new DocumentoSeed("Dev. Nota Fiscal Entrada", "PurchaseCreditNotes"),
                new DocumentoSeed("Dev. Nota Fiscal de Saída", "CreditNotes"),
                new DocumentoSeed("Devolução de mercadorias", "PurchaseReturns"),
                new DocumentoSeed("Pedido de Devolução de Mercadorias", "GoodsReturnRequest"),
                new DocumentoSeed("Cotação de Vendas", "Quotations"),
                new DocumentoSeed("Entrega", "DeliveryNotes"),
                new DocumentoSeed("Devoluções", "Returns"),
                new DocumentoSeed("Pedido de Devolução", "ReturnRequest"),
                new DocumentoSeed("Entrada de Mercadorias", "InventoryGenEntries"),
                new DocumentoSeed("Pedido de Transferência de Estoque", "InventoryTransferRequests"),
                new DocumentoSeed("Transferência do Estoque", "StockTransfers"),
                new DocumentoSeed("Saída de Mercadorias", "InventoryGenExits")
            };

            var endpoints = seeds.Select(x => x.Endpoint)
                                 .ToHashSet(StringComparer.Ordinal);

            var gruposDeDocumentos = (await dbContext.Documentos.ToListAsync(cancellationToken))
                .Where(x => endpoints.Contains(x.Endpoint))
                .GroupBy(x => x.Endpoint, StringComparer.Ordinal)
                .ToList();

            foreach (var grupoDuplicado in gruposDeDocumentos.Where(grupo => grupo.Count() > 1))
            {
                logger.LogWarning(
                    "Foram encontrados {Quantidade} documentos com o endpoint SAP {DocumentoEndpoint}; o registro mais antigo será usado pelo bootstrap.",
                    grupoDuplicado.Count(),
                    grupoDuplicado.Key);
            }

            var documentosExistentes = gruposDeDocumentos.ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.OrderBy(documento => documento.Id).First(),
                StringComparer.Ordinal);

            foreach (var seed in seeds)
            {
                if (documentosExistentes.ContainsKey(seed.Endpoint))
                {
                    logger.LogInformation("Documento padrão {DocumentoEndpoint} já existente.", seed.Endpoint);
                    continue;
                }

                var documento = new Documento
                {
                    NomeDocumento = seed.NomeDocumento,
                    Endpoint = seed.Endpoint
                };

                await dbContext.Documentos.AddAsync(documento, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                documentosExistentes[documento.Endpoint] = documento;
                logger.LogInformation("Documento padrão {DocumentoNome} criado com Id {DocumentoId}.", documento.NomeDocumento, documento.Id);
            }

            return documentosExistentes;
        }

        private static async Task<Dictionary<string, Colecao>> EnsureColecoesAsync(
            ExcelDocDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new ColecaoSeed("Cabeçalho Documentos de Marketing", "Document: campos do cabeçalho do documento.", TipoColecao.Header),
                new ColecaoSeed("DocumentLines", "Linhas do documento de marketing.", TipoColecao.Line),
                new ColecaoSeed("DocumentInstallments", "Parcelas do documento.", TipoColecao.Line),
                new ColecaoSeed("DocumentAdditionalExpenses", "Despesas adicionais do documento.", TipoColecao.Line),
                new ColecaoSeed("DocumentLineAdditionalExpenses", "Despesas adicionais vinculadas a uma DocumentLine.", TipoColecao.Line),
                new ColecaoSeed("DocumentSpecialLines", "Linhas especiais do documento.", TipoColecao.Line),
                new ColecaoSeed("DocumentLinesBinAllocations", "Alocações de posição vinculadas a uma DocumentLine.", TipoColecao.Line),
                new ColecaoSeed("BatchNumbers", "Lotes vinculados a uma DocumentLine.", TipoColecao.Line),
                new ColecaoSeed("SerialNumbers", "Números de série vinculados a uma DocumentLine.", TipoColecao.Line),
                new ColecaoSeed("WithholdingTaxDataCollection", "Dados de imposto retido do documento.", TipoColecao.Line),
                new ColecaoSeed("WithholdingTaxDataWTXCollection", "Dados WTX de imposto retido do documento.", TipoColecao.Line)
            };

            var nomes = seeds.Select(x => x.NomeColecao)
                             .ToHashSet(StringComparer.Ordinal);

            var gruposDeColecoes = (await dbContext.Colecoes
                    .Where(x => !x.FK_IdEmpresa.HasValue)
                    .ToListAsync(cancellationToken))
                .Where(x => nomes.Contains(x.NomeColecao))
                .GroupBy(x => x.NomeColecao, StringComparer.Ordinal)
                .ToList();

            foreach (var grupoDuplicado in gruposDeColecoes.Where(grupo => grupo.Count() > 1))
            {
                logger.LogWarning(
                    "Foram encontradas {Quantidade} coleções SAP globais com o nome {ColecaoNome}; o registro mais antigo será usado pelo bootstrap.",
                    grupoDuplicado.Count(),
                    grupoDuplicado.Key);
            }

            var colecoesExistentes = gruposDeColecoes.ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.OrderBy(colecao => colecao.Id).First(),
                StringComparer.Ordinal);

            foreach (var seed in seeds)
            {
                if (colecoesExistentes.TryGetValue(seed.NomeColecao, out var existente))
                {
                    existente.TipoColecao = seed.TipoColecao;
                    existente.Descricao = seed.Descricao;
                    logger.LogInformation("Coleção SAP global {ColecaoNome} já existente.", seed.NomeColecao);
                    continue;
                }

                var colecao = new Colecao
                {
                    NomeColecao = seed.NomeColecao,
                    Descricao = seed.Descricao,
                    TipoColecao = seed.TipoColecao,
                    FK_IdEmpresa = null
                };

                await dbContext.Colecoes.AddAsync(colecao, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                colecoesExistentes[colecao.NomeColecao] = colecao;
                logger.LogInformation("Coleção SAP global {ColecaoNome} criada com Id {ColecaoId}.", colecao.NomeColecao, colecao.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return colecoesExistentes;
        }

        private static async Task EnsureDocumentoColecoesAsync(ExcelDocDbContext dbContext, IReadOnlyDictionary<string, Documento> documentos,
                                                               IReadOnlyDictionary<string, Colecao> colecoes, ILogger logger,
                                                               CancellationToken cancellationToken)
        {
            var vinculosEsperados = MarketingDocumentEndpoints
                .SelectMany(endpoint => colecoes.Values.Select(colecao =>
                    (DocumentoId: documentos[endpoint].Id, ColecaoId: colecao.Id)))
                .Distinct()
                .ToList();

            var documentoIds = vinculosEsperados.Select(vinculo => vinculo.DocumentoId).Distinct().ToList();
            var colecaoIds = vinculosEsperados.Select(vinculo => vinculo.ColecaoId).Distinct().ToList();
            var vinculosExistentes = (await dbContext.DocumentoColecoes
                    .Where(vinculo =>
                        documentoIds.Contains(vinculo.FK_IdDocumento) &&
                        colecaoIds.Contains(vinculo.FK_IdColecao))
                    .Select(vinculo => new
                    {
                        DocumentoId = vinculo.FK_IdDocumento,
                        ColecaoId = vinculo.FK_IdColecao
                    })
                    .ToListAsync(cancellationToken))
                .Select(vinculo => (vinculo.DocumentoId, vinculo.ColecaoId))
                .ToHashSet();

            var novosVinculos = vinculosEsperados
                .Where(vinculo => !vinculosExistentes.Contains(vinculo))
                .Select(vinculo => new DocumentoColecao
                {
                    FK_IdDocumento = vinculo.DocumentoId,
                    FK_IdColecao = vinculo.ColecaoId
                })
                .ToList();

            if (novosVinculos.Count > 0)
            {
                await dbContext.DocumentoColecoes.AddRangeAsync(novosVinculos, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Catálogo SAP reconciliado: {TotalVinculos} vínculos documento-coleção verificados e {NovosVinculos} criados.",
                vinculosEsperados.Count,
                novosVinculos.Count);
        }

        private static async Task<Dictionary<string, Mapeamento>> EnsureMapeamentosAsync(
            ExcelDocDbContext dbContext,
            IReadOnlyDictionary<string, Colecao> colecoes,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var agora = DateTime.UtcNow;
            var seeds = new[]
            {
                new MapeamentoSeed(
                    MapeamentoCabecalhoNomePadrao,
                    "Cabeçalho Documentos de Marketing",
                    new[]
                    {
                        new MapeamentoCampoSeed("DocDate", "Data de Lançamento", 2, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("TaxDate", "Data de emissão", 3, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("DocDueDate", "Data de entrega", 4, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("BPL_IDAssignedToInvoice", "Id da Filial", 5, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("CardCode", "Id do parceiro", 6, TipoCampo.String, null),
                        new MapeamentoCampoSeed("SequenceCode", "Sequência do Documento", 9, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("SequenceSerial", "Número da NF", 10, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("Comments", "Observações", 38, TipoCampo.String, null)
                    }),
                new MapeamentoSeed(
                    MapeamentoDocumentLinesNomePadrao,
                    "DocumentLines",
                    new[]
                    {
                        new MapeamentoCampoSeed("ItemCode", "Código do Item", 13, TipoCampo.String, null),
                        new MapeamentoCampoSeed("LineTotal", "Valor Total da Linha", 14, TipoCampo.Double, null),
                        new MapeamentoCampoSeed("TaxCode", "Código de Imposto", 16, TipoCampo.String, null),
                        new MapeamentoCampoSeed("Quantity", "Quantidade", 35, TipoCampo.Double, null),
                        new MapeamentoCampoSeed("Usage", "Utilização", 17, TipoCampo.String, null),
                        new MapeamentoCampoSeed("CostingCode", "Centro de Custo", 18, TipoCampo.String, null),
                        new MapeamentoCampoSeed("AccountCode", "Conta Contábil", 19, TipoCampo.String, null),
                    }),
                new MapeamentoSeed(
                    MapeamentoParcelasNomePadrao,
                    "DocumentInstallments",
                    new[]
                    {
                        new MapeamentoCampoSeed("InstallmentId", "Id da Parcela", 20, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("DueDate", "Data de Vencimento da Parcela", 21, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("Total", "Total da Parcela",22, TipoCampo.Double, null),
                    }),
                new MapeamentoSeed(
                    "Mapeamento Padrão - DocumentAdditionalExpenses",
                    "DocumentAdditionalExpenses",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - DocumentLineAdditionalExpenses",
                    "DocumentLineAdditionalExpenses",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - DocumentSpecialLines",
                    "DocumentSpecialLines",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - DocumentLinesBinAllocations",
                    "DocumentLinesBinAllocations",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - BatchNumbers",
                    "BatchNumbers",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - SerialNumbers",
                    "SerialNumbers",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - WithholdingTaxDataCollection",
                    "WithholdingTaxDataCollection",
                    Array.Empty<MapeamentoCampoSeed>()),
                new MapeamentoSeed(
                    "Mapeamento Padrão - WithholdingTaxDataWTXCollection",
                    "WithholdingTaxDataWTXCollection",
                    Array.Empty<MapeamentoCampoSeed>())
            };

            var mapeamentosPadraoExistentes = await dbContext.Mapeamentos
                .Include(x => x.Campos)
                .Where(x => x.IsPadrao && !x.FK_IdEmpresa.HasValue)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var result = new Dictionary<string, Mapeamento>(StringComparer.Ordinal);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var seed in seeds)
                {
                    var colecao = colecoes[seed.ColecaoNome];
                    var mapeamento = mapeamentosPadraoExistentes.FirstOrDefault(existing =>
                            string.Equals(existing.Nome, seed.Nome, StringComparison.Ordinal))
                        ?? mapeamentosPadraoExistentes.FirstOrDefault(existing =>
                            existing.FK_IdColecao == colecao.Id &&
                            string.Equals(seed.ColecaoNome, "DocumentInstallments", StringComparison.Ordinal));

                    if (mapeamento is null)
                    {
                        mapeamento = new Mapeamento
                        {
                            Nome = seed.Nome,
                            FK_IdColecao = colecao.Id,
                            FK_IdEmpresa = null,
                            IsPadrao = true,
                            DataCriacao = agora,
                            Campos = seed.Campos.Select(fieldSeed => new MapeamentoCampo
                            {
                                NomeCampo = fieldSeed.NomeCampo,
                                DescricaoCampo = fieldSeed.DescricaoCampo,
                                IndiceColuna = fieldSeed.IndiceColuna,
                                TipoCampo = fieldSeed.TipoCampo,
                                Formato = fieldSeed.Formato,
                                Ativo = true
                            }).ToList()
                        };

                        await dbContext.Mapeamentos.AddAsync(mapeamento, cancellationToken);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        mapeamentosPadraoExistentes.Add(mapeamento);
                        logger.LogInformation("Mapeamento SAP global {MapeamentoNome} criado com Id {MapeamentoId}.", mapeamento.Nome, mapeamento.Id);
                    }
                    else
                    {
                        mapeamento.Nome = seed.Nome;
                        mapeamento.FK_IdColecao = colecao.Id;
                        mapeamento.FK_IdEmpresa = null;
                        mapeamento.IsPadrao = true;
                        var existingFields = mapeamento.Campos.ToList();
                        foreach (var fieldSeed in seed.Campos)
                        {
                            var field = existingFields.FirstOrDefault(candidate =>
                                string.Equals(candidate.NomeCampo, fieldSeed.NomeCampo, StringComparison.OrdinalIgnoreCase));

                            if (field is null)
                            {
                                if (existingFields.Any(candidate => candidate.IndiceColuna == fieldSeed.IndiceColuna))
                                {
                                    logger.LogWarning(
                                        "Campo SAP {CampoNome} não foi adicionado ao mapeamento {MapeamentoId}: o índice {IndiceColuna} já está ocupado.",
                                        fieldSeed.NomeCampo,
                                        mapeamento.Id,
                                        fieldSeed.IndiceColuna);
                                    continue;
                                }

                                field = new MapeamentoCampo
                                {
                                    FK_IdMapeamento = mapeamento.Id,
                                    NomeCampo = fieldSeed.NomeCampo,
                                    DescricaoCampo = fieldSeed.DescricaoCampo,
                                    IndiceColuna = fieldSeed.IndiceColuna,
                                    TipoCampo = fieldSeed.TipoCampo,
                                    Formato = fieldSeed.Formato,
                                    Ativo = true
                                };
                                await dbContext.MapeamentoCampos.AddAsync(field, cancellationToken);
                                existingFields.Add(field);
                                continue;
                            }

                            // Não reindexa nem remove campos existentes no startup. Apenas
                            // mantém os metadados do catálogo padrão e garante que ele esteja ativo.
                            field.NomeCampo = fieldSeed.NomeCampo;
                            field.DescricaoCampo = fieldSeed.DescricaoCampo;
                            field.TipoCampo = fieldSeed.TipoCampo;
                            field.Formato = fieldSeed.Formato;
                            field.Ativo = true;
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Mapeamento SAP global {MapeamentoNome} verificado com Id {MapeamentoId}.", mapeamento.Nome, mapeamento.Id);
                    }

                    result[seed.Nome] = mapeamento;
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task EnsurePerfilMapeamentoAsync(
            ExcelDocDbContext dbContext,
            IReadOnlyDictionary<string, Documento> documentos,
            IReadOnlyDictionary<string, Colecao> colecoes,
            IReadOnlyDictionary<string, Mapeamento> mapeamentos,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var documentosParaPerfil = MarketingDocumentEndpoints;

            var itens = new[]
            {
                new PerfilMapeamentoItemSeed("Cabeçalho Documentos de Marketing", MapeamentoCabecalhoNomePadrao),
                new PerfilMapeamentoItemSeed("DocumentLines", MapeamentoDocumentLinesNomePadrao),
                new PerfilMapeamentoItemSeed("DocumentInstallments", MapeamentoParcelasNomePadrao),
                new PerfilMapeamentoItemSeed("DocumentAdditionalExpenses", "Mapeamento Padrão - DocumentAdditionalExpenses"),
                new PerfilMapeamentoItemSeed(
                    "DocumentLineAdditionalExpenses",
                    "Mapeamento Padrão - DocumentLineAdditionalExpenses",
                    "DocumentLines"),
                new PerfilMapeamentoItemSeed("DocumentSpecialLines", "Mapeamento Padrão - DocumentSpecialLines"),
                new PerfilMapeamentoItemSeed(
                    "DocumentLinesBinAllocations",
                    "Mapeamento Padrão - DocumentLinesBinAllocations",
                    "DocumentLines"),
                new PerfilMapeamentoItemSeed("BatchNumbers", "Mapeamento Padrão - BatchNumbers", "DocumentLines"),
                new PerfilMapeamentoItemSeed("SerialNumbers", "Mapeamento Padrão - SerialNumbers", "DocumentLines"),
                new PerfilMapeamentoItemSeed(
                    "WithholdingTaxDataCollection",
                    "Mapeamento Padrão - WithholdingTaxDataCollection"),
                new PerfilMapeamentoItemSeed(
                    "WithholdingTaxDataWTXCollection",
                    "Mapeamento Padrão - WithholdingTaxDataWTXCollection")
            };

            foreach (var documentoEndpoint in documentosParaPerfil)
            {
                if (!documentos.TryGetValue(documentoEndpoint, out var documento))
                {
                    logger.LogInformation("Documento {DocumentoEndpoint} não encontrado para criação do perfil de mapeamento.", documentoEndpoint);
                    continue;
                }

                var perfil = await dbContext.PerfilMapeamentos
                    .Include(x => x.Itens)
                    .FirstOrDefaultAsync(
                        x => x.IsPadrao &&
                             !x.FK_IdEmpresa.HasValue &&
                             x.FK_IdDocumento == documento.Id &&
                             x.Nome == PerfilDocumentosDeMarketingPadrao,
                        cancellationToken);

                if (perfil is null)
                {
                    perfil = new PerfilMapeamento
                    {
                        Nome = PerfilDocumentosDeMarketingPadrao,
                        FK_IdDocumento = documento.Id,
                        FK_IdEmpresa = null,
                        IsPadrao = true,
                        DataCriacao = DateTime.UtcNow
                    };

                    await dbContext.PerfilMapeamentos.AddAsync(perfil, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Perfil de mapeamento padrão {PerfilNome} criado para o documento {DocumentoId} com Id {PerfilId}.", perfil.Nome, documento.Id, perfil.Id);
                }
                else
                {
                    perfil.Nome = PerfilDocumentosDeMarketingPadrao;
                    perfil.IsPadrao = true;
                    perfil.FK_IdEmpresa = null;
                    logger.LogInformation("Perfil de mapeamento SAP global {PerfilNome} verificado para o documento {DocumentoId} com Id {PerfilId}.", perfil.Nome, documento.Id, perfil.Id);
                }

                foreach (var itemSeed in itens)
                {
                    var colecao = colecoes[itemSeed.ColecaoNome];
                    var mapeamento = mapeamentos[itemSeed.MapeamentoNome];
                    var item = perfil.Itens.FirstOrDefault(existingItem => existingItem.FK_IdColecao == colecao.Id);

                    if (item is null)
                    {
                        item = new PerfilMapeamentoItem
                        {
                            FK_IdPerfilMapeamento = perfil.Id,
                            FK_IdColecao = colecao.Id
                        };
                        perfil.Itens.Add(item);
                    }

                    item.FK_IdMapeamento = mapeamento.Id;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var itensPorColecao = perfil.Itens.ToDictionary(item => item.FK_IdColecao);
                foreach (var itemSeed in itens)
                {
                    var item = itensPorColecao[colecoes[itemSeed.ColecaoNome].Id];
                    item.FK_IdPerfilMapeamentoItemPai = itemSeed.ColecaoPaiNome is null
                        ? null
                        : itensPorColecao[colecoes[itemSeed.ColecaoPaiNome].Id].Id;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Perfil SAP {PerfilId} do documento {DocumentoEndpoint} reconciliado com {TotalEstruturas} estruturas.",
                    perfil.Id,
                    documentoEndpoint,
                    itens.Length);
            }
        }

        private static async Task EnsureUsuarioPadraoAsync(
            ExcelDocDbContext dbContext,
            IPasswordHasherService passwordHasherService,
            int empresaId,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var usuarioExiste = await dbContext.Usuarios.AnyAsync(
                x => x.NomeUsuario == UsuarioNomePadrao || (x.Email != null && x.Email == UsuarioEmailPadrao),
                cancellationToken);

            if (usuarioExiste)
            {
                logger.LogInformation("Usuário padrão {UsuarioNome} já existente. Seed ignorado.", UsuarioNomePadrao);
                return;
            }

            var usuario = new Usuario
            {
                NomeUsuario = UsuarioNomePadrao,
                SenhaHash = passwordHasherService.Hash(UsuarioSenhaPadrao),
                Email = UsuarioEmailPadrao,
                TipoUsuario = TipoUsuario.Administrador,
                FK_IdEmpresa = empresaId,
                Ativo = true
            };

            await dbContext.Usuarios.AddAsync(usuario, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Usuário padrão {UsuarioNome} criado com Id {UsuarioId} vinculado à empresa {EmpresaId}.", usuario.NomeUsuario, usuario.Id, empresaId);
        }

        private sealed record DocumentoSeed(string NomeDocumento, string Endpoint);

        private sealed record ColecaoSeed(string NomeColecao, string Descricao, TipoColecao TipoColecao);

        private sealed record MapeamentoSeed(string Nome, string ColecaoNome, IReadOnlyCollection<MapeamentoCampoSeed> Campos);

        private sealed record MapeamentoCampoSeed(string NomeCampo, string DescricaoCampo, int IndiceColuna, TipoCampo TipoCampo, string? Formato);

        private sealed record PerfilMapeamentoItemSeed(
            string ColecaoNome,
            string MapeamentoNome,
            string? ColecaoPaiNome = null);
    }
}
