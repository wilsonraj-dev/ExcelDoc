using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Models;
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

        public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var passwordHasherService = scopedProvider.GetRequiredService<IPasswordHasherService>();
            var encryptionService = scopedProvider.GetRequiredService<IEncryptionService>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            await dbContext.Database.MigrateAsync(cancellationToken);

            var empresa = await EnsureEmpresaAsync(dbContext, logger, cancellationToken);
            await EnsureConfiguracaoAsync(dbContext, encryptionService, empresa.Id, logger, cancellationToken);

            var documentos = await EnsureDocumentosAsync(dbContext, logger, cancellationToken);
            var colecoes = await EnsureColecoesAsync(dbContext, empresa.Id, logger, cancellationToken);

            await EnsureDocumentoColecoesAsync(dbContext, documentos, colecoes, logger, cancellationToken);

            var mapeamentos = await EnsureMapeamentosAsync(dbContext, empresa.Id, colecoes, logger, cancellationToken);
            await EnsurePerfilMapeamentoAsync(dbContext, empresa.Id, documentos, colecoes, mapeamentos, logger, cancellationToken);

            await EnsureUsuarioPadraoAsync(dbContext, passwordHasherService, empresa.Id, logger, cancellationToken);
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
                UsuarioBanco = encryptionService.Encrypt("Teste"),
                SenhaBanco = encryptionService.Encrypt("Teste"),
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

            var colecoesExistentes = (await dbContext.Colecoes.Where(x => x.FK_IdEmpresa == empresaId)
                                                              .ToListAsync(cancellationToken))
                                                              .Where(x => nomes.Contains(x.NomeColecao))
                                                              .ToDictionary(x => x.NomeColecao);

            foreach (var seed in seeds)
            {
                if (colecoesExistentes.ContainsKey(seed.NomeColecao))
                {
                    logger.LogInformation("Coleção padrão {ColecaoNome} já existente para a empresa {EmpresaId}.", seed.NomeColecao, empresaId);
                    continue;
                }

                var colecao = new Colecao
                {
                    NomeColecao = seed.NomeColecao,
                    TipoColecao = seed.TipoColecao,
                    //FK_IdEmpresa = empresaId
                };

                await dbContext.Colecoes.AddAsync(colecao, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                colecoesExistentes[colecao.NomeColecao] = colecao;
                logger.LogInformation("Coleção padrão {ColecaoNome} criada com Id {ColecaoId}.", colecao.NomeColecao, colecao.Id);
            }

            return colecoesExistentes;
        }

        private static async Task EnsureDocumentoColecoesAsync(ExcelDocDbContext dbContext, IReadOnlyDictionary<string, Documento> documentos,
                                                               IReadOnlyDictionary<string, Colecao> colecoes, ILogger logger,
                                                               CancellationToken cancellationToken)
        {
            var seeds = new[]
            {
                new DocumentoColecaoSeed("PurchaseOrders", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("Orders", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("PurchaseInvoices", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("Invoices", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("PurchaseCreditNotes", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("CreditNotes", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("GoodsReturnRequest", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("Quotations", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("ReturnRequest", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("PurchaseDownPayments", "Cabeçalho Documentos de Marketing"),
                new DocumentoColecaoSeed("DownPayments", "Cabeçalho Documentos de Marketing"),

                new DocumentoColecaoSeed("PurchaseOrders", "DocumentLines"),
                new DocumentoColecaoSeed("Orders", "DocumentLines"),
                new DocumentoColecaoSeed("PurchaseInvoices", "DocumentLines"),
                new DocumentoColecaoSeed("Invoices", "DocumentLines"),
                new DocumentoColecaoSeed("PurchaseCreditNotes", "DocumentLines"),
                new DocumentoColecaoSeed("CreditNotes", "DocumentLines"),
                new DocumentoColecaoSeed("GoodsReturnRequest", "DocumentLines"),
                new DocumentoColecaoSeed("Quotations", "DocumentLines"),
                new DocumentoColecaoSeed("ReturnRequest", "DocumentLines"),
                new DocumentoColecaoSeed("PurchaseDownPayments", "DocumentLines"),
                new DocumentoColecaoSeed("DownPayments", "DocumentLines"),

                new DocumentoColecaoSeed("PurchaseOrders", "DocumentInstallments"),
                new DocumentoColecaoSeed("Orders", "DocumentInstallments"),
                new DocumentoColecaoSeed("PurchaseInvoices", "DocumentInstallments"),
                new DocumentoColecaoSeed("Invoices", "DocumentInstallments"),
                new DocumentoColecaoSeed("PurchaseCreditNotes", "DocumentInstallments"),
                new DocumentoColecaoSeed("CreditNotes", "DocumentInstallments"),
                new DocumentoColecaoSeed("GoodsReturnRequest", "DocumentInstallments"),
                new DocumentoColecaoSeed("Quotations", "DocumentInstallments"),
                new DocumentoColecaoSeed("ReturnRequest", "DocumentInstallments"),
                new DocumentoColecaoSeed("PurchaseDownPayments", "DocumentInstallments"),
                new DocumentoColecaoSeed("DownPayments", "DocumentInstallments"),
            };

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
                        new MapeamentoCampoSeed("CardCode", "Id do parceiro", 5, TipoCampo.String, null),
                        new MapeamentoCampoSeed("DocDate", "Data de Lançamento", 2, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("TaxDate", "Data de emissão", 3, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("DocDueDate", "Data de entrega", 4, TipoCampo.DateTime, "yyyy-MM-dd"),
                        new MapeamentoCampoSeed("BPL_IDAssignedToInvoice", "Id da Filial", 5, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("SequenceCode", "Sequência do Documento", 6, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("SequenceSerial", "Número da NF", 7, TipoCampo.Int, null),
                        new MapeamentoCampoSeed("Comments", "Observações", 36, TipoCampo.String, null)
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

            var mapeamentosExistentes = (await dbContext.Mapeamentos.Where(x => x.FK_IdEmpresa == empresaId)
                                                                    .ToListAsync(cancellationToken))
                                                                    .Where(x => nomes.Contains(x.Nome))
                                                                    .ToDictionary(x => x.Nome);

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
                    logger.LogInformation("Mapeamento padrão {MapeamentoNome} já existente com Id {MapeamentoId}.", mapeamento.Nome, mapeamento.Id);
                }

                foreach (var campoSeed in seed.Campos)
                {
                    var campoExiste = await dbContext.MapeamentoCampos.AnyAsync(
                        x => x.FK_IdMapeamento == mapeamento.Id && x.IndiceColuna == campoSeed.IndiceColuna,
                        cancellationToken);

                    if (campoExiste)
                    {
                        logger.LogInformation(
                            "Campo padrão do mapeamento {MapeamentoId} na coluna {IndiceColuna} já existente.",
                            mapeamento.Id,
                            campoSeed.IndiceColuna);
                        continue;
                    }

                    await dbContext.MapeamentoCampos.AddAsync(new MapeamentoCampo
                    {
                        NomeCampo = campoSeed.NomeCampo,
                        DescricaoCampo = campoSeed.DescricaoCampo,
                        IndiceColuna = campoSeed.IndiceColuna,
                        TipoCampo = campoSeed.TipoCampo,
                        Formato = campoSeed.Formato,
                        FK_IdMapeamento = mapeamento.Id
                    }, cancellationToken);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Campo padrão {NomeCampo} criado para o mapeamento {MapeamentoId}.",
                        campoSeed.NomeCampo,
                        mapeamento.Id);
                }
            }

            return mapeamentosExistentes;
        }

        private static async Task EnsurePerfilMapeamentoAsync(ExcelDocDbContext dbContext, int empresaId,
                                                              IReadOnlyDictionary<string, Documento> documentos,
                                                              IReadOnlyDictionary<string, Colecao> colecoes,
                                                              IReadOnlyDictionary<string, Mapeamento> mapeamentos,
                                                              ILogger logger, CancellationToken cancellationToken)
        {
            var documentosParaPerfil = new[]
            {
                "PurchaseOrders",
                "Orders",
                "PurchaseInvoices",
                "Invoices",
                "PurchaseCreditNotes",
                "CreditNotes",
                "GoodsReturnRequest",
                "Quotations",
                "ReturnRequest",
                "PurchaseDownPayments",
                "DownPayments"
            };

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

                var perfil = await dbContext.PerfilMapeamentos.FirstOrDefaultAsync(
                    x => x.FK_IdEmpresa == empresaId && x.FK_IdDocumento == documento.Id && x.Nome == PerfilDocumentosDeMarketingPadrao,
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
                    logger.LogInformation("Perfil de mapeamento padrão {PerfilNome} já existente para o documento {DocumentoId} com Id {PerfilId}.", perfil.Nome, documento.Id, perfil.Id);
                }

                foreach (var itemSeed in itens)
                {
                    var colecao = colecoes[itemSeed.ColecaoNome];
                    var mapeamento = mapeamentos[itemSeed.MapeamentoNome];
                    var itemExiste = await dbContext.PerfilMapeamentoItens.AnyAsync(
                        x => x.FK_IdPerfilMapeamento == perfil.Id && x.FK_IdColecao == colecao.Id,
                        cancellationToken);

                    if (itemExiste)
                    {
                        logger.LogInformation(
                            "Item do perfil {PerfilId} para a coleção {ColecaoId} já existente.",
                            perfil.Id,
                            colecao.Id);
                        continue;
                    }

                    await dbContext.PerfilMapeamentoItens.AddAsync(new PerfilMapeamentoItem
                    {
                        FK_IdPerfilMapeamento = perfil.Id,
                        FK_IdColecao = colecao.Id,
                        FK_IdMapeamento = mapeamento.Id
                    }, cancellationToken);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Item do perfil {PerfilId} para a coleção {ColecaoId} criado.",
                        perfil.Id,
                        colecao.Id);
                }
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
