using ExcelDoc.Server.DTOs.PerfilMapeamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelDoc.Server.Tests;

public sealed class PerfilMapeamentoAuthorizationTests
{
    [Fact]
    public async Task GetByDocumentoAsync_ReturnsDefaultAndCustomProfiles()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis =
            [
                CreateProfile(1, "Padrão SAP", isDefault: true),
                CreateProfile(2, "Perfil personalizado A"),
                CreateProfile(3, "Perfil personalizado B")
            ]
        };
        var service = CreateService(repository);

        var result = await service.GetByDocumentoAsync(100);

        Assert.Equal([1, 2, 3], result.Select(profile => profile.Id));
        Assert.True(result.Single(profile => profile.Id == 1).IsPadrao);
        Assert.False(result.Single(profile => profile.Id == 2).IsPadrao);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCustomProfile()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis = [CreateProfile(3, "Perfil personalizado")]
        };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(3);

        Assert.Equal(3, result.Id);
        Assert.False(result.IsPadrao);
    }

    [Fact]
    public async Task AtualizarAsync_RejectsDefaultProfileForRegularUser()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis = [CreateProfile(1, "Padrão SAP", isDefault: true)]
        };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AtualizarAsync(1, new PerfilMapeamentoRequestDto
            {
                Nome = "Novo nome",
                FK_IdDocumento = 100,
                IsPadrao = true,
                Itens = []
            }));
    }

    private static PerfilMapeamentoService CreateService(
        StubPerfilMapeamentoRepository repository) =>
        new(
            repository,
            new StubMessageService(),
            new StubUsuarioAcessoService(new Usuario
            {
                Id = 10,
                TipoUsuario = TipoUsuario.Usuario
            }),
            NullLogger<PerfilMapeamentoService>.Instance);

    private static PerfilMapeamento CreateProfile(
        int id,
        string name,
        bool isDefault = false) =>
        new()
        {
            Id = id,
            Nome = name,
            FK_IdDocumento = 100,
            IsPadrao = isDefault,
            Documento = new Documento { Id = 100 },
            Itens = []
        };
}
