using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services;

public sealed class UsuarioAcessoService : IUsuarioAcessoService
{
    private readonly ICurrentUserService _currentUserService;

    public UsuarioAcessoService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Task<Usuario> GetUsuarioAtualAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_currentUserService.GetRequiredUser());
    }
}
