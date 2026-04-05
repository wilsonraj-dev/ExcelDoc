using ExcelDoc.Server.Background;
using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.Repositories;
using ExcelDoc.Server.Repositories.Interfaces;
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
            services.AddHttpContextAccessor();

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
            services.AddScoped<IDocumentoRepository, DocumentoRepository>();
            services.AddScoped<IColecaoRepository, ColecaoRepository>();
            services.AddScoped<IProcessamentoRepository, ProcessamentoRepository>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IHashArquivoService, HashArquivoService>();
            services.AddScoped<IArquivoStorageService, ArquivoStorageService>();
            services.AddScoped<IExcelReaderService, ExcelReaderService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUsuarioAcessoService, UsuarioAcessoService>();
            services.AddScoped<IEmpresaService, EmpresaService>();
            services.AddScoped<IDocumentoService, DocumentoService>();
            services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
            services.AddScoped<IColecaoService, ColecaoService>();
            services.AddScoped<IPayloadBuilderService, PayloadBuilderService>();
            services.AddScoped<ISapServiceLayerClient, SapServiceLayerClient>();
            services.AddScoped<IProcessamentoService, ProcessamentoService>();
            services.AddScoped<IProcessamentoWorkerService, ProcessamentoWorkerService>();
            services.AddHostedService<QueuedProcessingHostedService>();

            return services;
        }
    }
}
