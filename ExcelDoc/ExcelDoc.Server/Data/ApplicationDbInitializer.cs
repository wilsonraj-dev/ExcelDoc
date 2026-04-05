using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Data
{
    public static class ApplicationDbInitializer
    {
        private const string EmpresaNomePadrao = "B2Finance";
        private const string UsuarioNomePadrao = "Wilson";
        private const string UsuarioSenhaPadrao = "B1@Admin";
        private const string UsuarioEmailPadrao = "wilson.assis.junior@gmail.com";

        public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<ExcelDocDbContext>();
            var passwordHasherService = scopedProvider.GetRequiredService<IPasswordHasherService>();
            var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ApplicationDbInitializer");

            await dbContext.Database.MigrateAsync(cancellationToken);

            var empresa = await dbContext.Empresas
                .FirstOrDefaultAsync(x => x.NomeEmpresa == EmpresaNomePadrao, cancellationToken);

            if (empresa is null)
            {
                empresa = new Empresa
                {
                    NomeEmpresa = EmpresaNomePadrao
                };

                await dbContext.Empresas.AddAsync(empresa, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Empresa padrão {EmpresaNome} criada com Id {EmpresaId}.", empresa.NomeEmpresa, empresa.Id);
            }
            else
            {
                logger.LogInformation("Empresa padrão {EmpresaNome} já existente com Id {EmpresaId}.", empresa.NomeEmpresa, empresa.Id);
            }

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
                FK_IdEmpresa = empresa.Id,
                Ativo = true
            };

            await dbContext.Usuarios.AddAsync(usuario, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Usuário padrão {UsuarioNome} criado com Id {UsuarioId} vinculado à empresa {EmpresaId}.", usuario.NomeUsuario, usuario.Id, empresa.Id);
        }
    }
}
