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
        private const string MapeamentoParcelasNomePadrao = "Mapeamento Padrão - Parcelas";
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

        public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var passwordHasherService = scopedProvider.GetRequiredService<IPasswordHasherService>();
            var encryptionService = scopedProvider.GetRequiredService<IEncryptionService>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            await dbContext.Database.MigrateAsync(cancellationToken);
            await EnsurePerfilMapeamentoItemPaiSchemaAsync(dbContext, logger, cancellationToken);

            var empresa = await EnsureEmpresaAsync(dbContext, logger, cancellationToken);
            await EnsureConfiguracaoAsync(dbContext, encryptionService, empresa.Id, logger, cancellationToken);

            var documentos = await EnsureDocumentosAsync(dbContext, logger, cancellationToken);
            var colecoes = await EnsureColecoesAsync(dbContext, empresa.Id, logger, cancellationToken);

            await EnsureDocumentoColecoesAsync(dbContext, documentos, colecoes, logger, cancellationToken);

            var mapeamentos = await EnsureMapeamentosAsync(dbContext, empresa.Id, colecoes, logger, cancellationToken);
            await EnsurePerfilMapeamentoAsync(dbContext, empresa.Id, documentos, colecoes, mapeamentos, logger, cancellationToken);

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

            var documentosExistentes = (await dbContext.Documentos.ToListAsync(cancellationToken))
                                                                  .Where(x => endpoints.Contains(x.Endpoint))
                                                                  .ToDictionary(x => x.Endpoint);

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

        private static async Task<Dictionary<string, Colecao>> EnsureColecoesAsync(ExcelDocDbContext dbContext, int empresaId,
                                                                                   ILogger logger, CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new ColecaoSeed("Cabeçalho Documentos de Marketing", TipoColecao.Header),
                new ColecaoSeed("DocumentLines", TipoColecao.Line),
                new ColecaoSeed("DocumentInstallments", TipoColecao.Line),
            };

            var nomes = seeds.Select(x => x.NomeColecao)
                             .ToHashSet(StringComparer.Ordinal);

            var colecoesExistentes = (await dbContext.Colecoes
                    .Where(x => !x.FK_IdEmpresa.HasValue || x.FK_IdEmpresa == empresaId)
                    .ToListAsync(cancellationToken))
                .Where(x => nomes.Contains(x.NomeColecao))
                .GroupBy(x => x.NomeColecao, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(x => x.FK_IdEmpresa.HasValue).ThenBy(x => x.Id).First(),
                    StringComparer.Ordinal);

            foreach (var seed in seeds)
            {
                if (colecoesExistentes.ContainsKey(seed.NomeColecao))
                {
                    colecoesExistentes[seed.NomeColecao].TipoColecao = seed.TipoColecao;
                    logger.LogInformation("Coleção padrão {ColecaoNome} já existente para a empresa {EmpresaId}.", seed.NomeColecao, empresaId);
                    continue;
                }

                var colecao = new Colecao
                {
                    NomeColecao = seed.NomeColecao,
                    TipoColecao = seed.TipoColecao,
                    FK_IdEmpresa = null
                };

                await dbContext.Colecoes.AddAsync(colecao, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                colecoesExistentes[colecao.NomeColecao] = colecao;
                logger.LogInformation("Coleção padrão {ColecaoNome} criada com Id {ColecaoId}.", colecao.NomeColecao, colecao.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return colecoesExistentes;
        }

        private static async Task EnsureDocumentoColecoesAsync(ExcelDocDbContext dbContext, IReadOnlyDictionary<string, Documento> documentos,
                                                               IReadOnlyDictionary<string, Colecao> colecoes, ILogger logger,
                                                               CancellationToken cancellationToken)
        {
            var collectionNames = new[]
            {
                "Cabeçalho Documentos de Marketing",
                "DocumentLines",
                "DocumentInstallments"
            };

            var seeds = MarketingDocumentEndpoints
                .SelectMany(endpoint => collectionNames.Select(collectionName =>
                    new DocumentoColecaoSeed(endpoint, collectionName)))
                .ToList();

            foreach (var seed in seeds)
            {
                var documento = documentos[seed.DocumentoEndpoint];
                var colecao = colecoes[seed.ColecaoNome];

                var relacionamentoExiste = await dbContext.DocumentoColecoes.AnyAsync(
                    x => x.FK_IdDocumento == documento.Id && x.FK_IdColecao == colecao.Id,
                    cancellationToken);

                if (relacionamentoExiste)
                {
                    logger.LogInformation(
                        "Vínculo entre documento {DocumentoId} e coleção {ColecaoId} já existente.",
                        documento.Id,
                        colecao.Id);
                    continue;
                }

                await dbContext.DocumentoColecoes.AddAsync(new DocumentoColecao
                {
                    FK_IdDocumento = documento.Id,
                    FK_IdColecao = colecao.Id
                }, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Vínculo entre documento {DocumentoId} e coleção {ColecaoId} criado.",
                    documento.Id,
                    colecao.Id);
            }
        }

        private static async Task<Dictionary<string, Mapeamento>> EnsureMapeamentosAsync(ExcelDocDbContext dbContext, int empresaId,
                                                                                         IReadOnlyDictionary<string, Colecao> colecoes,
                                                                                         ILogger logger, CancellationToken cancellationToken)
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
                    })
            };

            var nomes = seeds.Select(x => x.Nome)
                             .ToHashSet(StringComparer.Ordinal);

            var mapeamentosExistentes = (await dbContext.Mapeamentos
                    .Include(x => x.Campos)
                    .Where(x => x.FK_IdEmpresa == empresaId)
                    .ToListAsync(cancellationToken))
                .Where(x => nomes.Contains(x.Nome))
                .GroupBy(x => x.Nome, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.OrderBy(x => x.Id).First(), StringComparer.Ordinal);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var seed in seeds)
                {
                    if (!mapeamentosExistentes.TryGetValue(seed.Nome, out var mapeamento))
                    {
                        mapeamento = new Mapeamento
                        {
                            Nome = seed.Nome,
                            FK_IdColecao = colecoes[seed.ColecaoNome].Id,
                            FK_IdEmpresa = empresaId,
                            IsPadrao = true,
                            DataCriacao = agora
                        };

                        await dbContext.Mapeamentos.AddAsync(mapeamento, cancellationToken);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        mapeamentosExistentes[mapeamento.Nome] = mapeamento;
                        logger.LogInformation("Mapeamento padrão {MapeamentoNome} criado com Id {MapeamentoId}.", mapeamento.Nome, mapeamento.Id);
                    }
                    else
                    {
                        mapeamento.FK_IdColecao = colecoes[seed.ColecaoNome].Id;
                        mapeamento.FK_IdEmpresa = empresaId;
                        mapeamento.IsPadrao = true;
                        logger.LogInformation("Mapeamento padrão {MapeamentoNome} reconciliado com Id {MapeamentoId}.", mapeamento.Nome, mapeamento.Id);
                    }

                    var existingFields = mapeamento.Campos.ToList();
                    if (existingFields.Count > 0)
                    {
                        var temporaryStart = Math.Max(
                            existingFields.Max(field => field.IndiceColuna),
                            seed.Campos.Max(field => field.IndiceColuna)) + 10_000;

                        foreach (var indexedField in existingFields.OrderBy(field => field.Id).Select((field, index) => (field, index)))
                        {
                            indexedField.field.IndiceColuna = temporaryStart + indexedField.index;
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);
                    }

                    var retainedFields = new HashSet<MapeamentoCampo>();
                    foreach (var fieldSeed in seed.Campos)
                    {
                        var field = existingFields.FirstOrDefault(candidate =>
                            !retainedFields.Contains(candidate) &&
                            string.Equals(candidate.NomeCampo, fieldSeed.NomeCampo, StringComparison.OrdinalIgnoreCase));

                        if (field is null)
                        {
                            field = new MapeamentoCampo
                            {
                                FK_IdMapeamento = mapeamento.Id
                            };
                            await dbContext.MapeamentoCampos.AddAsync(field, cancellationToken);
                        }

                        field.NomeCampo = fieldSeed.NomeCampo;
                        field.DescricaoCampo = fieldSeed.DescricaoCampo;
                        field.IndiceColuna = fieldSeed.IndiceColuna;
                        field.TipoCampo = fieldSeed.TipoCampo;
                        field.Formato = fieldSeed.Formato;
                        retainedFields.Add(field);
                    }

                    var obsoleteFields = existingFields.Where(field => !retainedFields.Contains(field)).ToList();
                    dbContext.MapeamentoCampos.RemoveRange(obsoleteFields);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return mapeamentosExistentes;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task EnsurePerfilMapeamentoAsync(ExcelDocDbContext dbContext, int empresaId,
                                                              IReadOnlyDictionary<string, Documento> documentos,
                                                              IReadOnlyDictionary<string, Colecao> colecoes,
                                                              IReadOnlyDictionary<string, Mapeamento> mapeamentos,
                                                              ILogger logger, CancellationToken cancellationToken)
        {
            var documentosParaPerfil = MarketingDocumentEndpoints;

            var itens = new[]
            {
                new PerfilMapeamentoItemSeed("Cabeçalho Documentos de Marketing", MapeamentoCabecalhoNomePadrao),
                new PerfilMapeamentoItemSeed("DocumentLines", MapeamentoDocumentLinesNomePadrao),
                new PerfilMapeamentoItemSeed("DocumentInstallments", MapeamentoParcelasNomePadrao)
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
                        x => x.FK_IdEmpresa == empresaId &&
                             x.FK_IdDocumento == documento.Id &&
                             x.Nome == PerfilDocumentosDeMarketingPadrao,
                        cancellationToken);

                if (perfil is null)
                {
                    perfil = new PerfilMapeamento
                    {
                        Nome = PerfilDocumentosDeMarketingPadrao,
                        FK_IdDocumento = documento.Id,
                        FK_IdEmpresa = empresaId,
                        IsPadrao = true,
                        DataCriacao = DateTime.UtcNow
                    };

                    await dbContext.PerfilMapeamentos.AddAsync(perfil, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Perfil de mapeamento padrão {PerfilNome} criado para o documento {DocumentoId} com Id {PerfilId}.", perfil.Nome, documento.Id, perfil.Id);
                }
                else
                {
                    perfil.IsPadrao = true;
                    perfil.FK_IdEmpresa = empresaId;
                    logger.LogInformation("Perfil de mapeamento padrão {PerfilNome} reconciliado para o documento {DocumentoId} com Id {PerfilId}.", perfil.Nome, documento.Id, perfil.Id);
                }

                foreach (var existingItem in perfil.Itens)
                {
                    existingItem.FK_IdPerfilMapeamentoItemPai = null;
                    existingItem.ItemPai = null;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var retainedItems = new HashSet<PerfilMapeamentoItem>();
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
                    item.FK_IdPerfilMapeamentoItemPai = null;
                    retainedItems.Add(item);
                    logger.LogInformation(
                        "Item do perfil {PerfilId} para a coleção {ColecaoId} reconciliado.",
                        perfil.Id,
                        colecao.Id);
                }

                var obsoleteItems = perfil.Itens.Where(item => !retainedItems.Contains(item)).ToList();
                dbContext.PerfilMapeamentoItens.RemoveRange(obsoleteItems);
                await dbContext.SaveChangesAsync(cancellationToken);
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

        private sealed record ColecaoSeed(string NomeColecao, TipoColecao TipoColecao);

        private sealed record DocumentoColecaoSeed(string DocumentoEndpoint, string ColecaoNome);

        private sealed record MapeamentoSeed(string Nome, string ColecaoNome, IReadOnlyCollection<MapeamentoCampoSeed> Campos);

        private sealed record MapeamentoCampoSeed(string NomeCampo, string DescricaoCampo, int IndiceColuna, TipoCampo TipoCampo, string? Formato);

        private sealed record PerfilMapeamentoItemSeed(string ColecaoNome, string MapeamentoNome);
    }
}
