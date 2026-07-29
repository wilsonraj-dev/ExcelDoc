using ExcelDoc.Server.Background;
using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.Repositories;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.IoC
{
    public static class DependencyInjectionServices
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<ISapSessionStore, SapSessionStore>();
            services.AddSingleton<ISapServiceLayerClient, SapServiceLayerClient>();
            services.AddHttpContextAccessor();

            services.AddScoped<IDocumentoRepository, DocumentoRepository>();
            services.AddScoped<IColecaoRepository, ColecaoRepository>();
            services.AddScoped<IMapeamentoRepository, MapeamentoRepository>();
            services.AddScoped<IProcessamentoRepository, ProcessamentoRepository>();
            services.AddScoped<IPerfilMapeamentoRepository, PerfilMapeamentoRepository>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddScoped<ISapSessionContextAccessor, SapSessionContextAccessor>();
            services.AddScoped<IHashArquivoService, HashArquivoService>();
            services.AddScoped<IArquivoStorageService, ArquivoStorageService>();
            services.AddScoped<IExcelReaderService, ExcelReaderService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUsuarioAcessoService, UsuarioAcessoService>();
            services.AddScoped<ISapDatabaseInitializer, SapDatabaseInitializer>();
            services.AddScoped<ISapUdtStore, SapUdtStore>();
            services.AddScoped<SapSchemaInstaller>();
            services.AddScoped<IDocumentoService, DocumentoService>();
            services.AddScoped<IColecaoService, ColecaoService>();
            services.AddScoped<IMapeamentoService, MapeamentoService>();
            services.AddScoped<IMapeamentoCampoService, MapeamentoCampoService>();
            services.AddScoped<IPayloadBuilderService, PayloadBuilderService>();
            services.AddScoped<IJsonBuilderService, JsonBuilderService>();
            services.AddScoped<IDocumentoUnicoService, DocumentoUnicoService>();
            services.AddScoped<IAgrupamentoService, AgrupamentoService>();
            services.AddScoped<IProcessamentoService, ProcessamentoService>();
            services.AddScoped<IPerfilMapeamentoService, PerfilMapeamentoService>();
            services.AddScoped<IProcessamentoWorkerService, ProcessamentoWorkerService>();
            services.AddHostedService<QueuedProcessingHostedService>();
            services.AddHostedService<SapSessionCleanupHostedService>();

            return services;
        }
    }
}
