using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace ExcelDoc.Server.IoC
{
    public static class DependencyInjectionLanguages
    {
        public static IServiceCollection AddInfrastructureLanguages(this IServiceCollection services)
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("pt-BR"),
                    new CultureInfo("en-US"),
                    new CultureInfo("es-ES")
                };

                options.DefaultRequestCulture = new RequestCulture("pt-BR");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;
                options.RequestCultureProviders =
                [
                    new AcceptLanguageHeaderRequestCultureProvider()
                ];
            });

            return services;
        }
    }
}
