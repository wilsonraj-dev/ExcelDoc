using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelDoc.Server.Tests;

public sealed class PerfilMapeamentoTenantTests
{
    [Fact]
    public async Task GetByDocumentoAsync_ReturnsDefaultsAndOnlyCurrentCompanyClones()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis =
            [
                CreateProfile(1, "Padrão SAP", null, isDefault: true),
                CreateProfile(2, "Clone Empresa 7", 7),
                CreateProfile(3, "Clone Empresa 8", 8)
            ]
        };
        var service = CreateService(repository, companyId: 7);

        var result = await service.GetByDocumentoAsync(100);

        Assert.Equal([1, 2], result.Select(profile => profile.Id));
    }

    [Fact]
    public async Task GetByDocumentoAsync_TreatsLegacyCompanyDefaultAsTenantProfile()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis =
            [
                CreateProfile(1, "PadrÃ£o SAP", null, isDefault: true),
                CreateProfile(2, "Legado Empresa 7", 7, isDefault: true),
                CreateProfile(3, "Legado Empresa 8", 8, isDefault: true)
            ]
        };
        var service = CreateService(repository, companyId: 7);

        var result = await service.GetByDocumentoAsync(100);

        Assert.Equal([1, 2], result.Select(profile => profile.Id));
        Assert.True(result.Single(profile => profile.Id == 1).IsPadrao);
        Assert.False(result.Single(profile => profile.Id == 2).IsPadrao);
    }

    [Fact]
    public async Task GetByIdAsync_RejectsCloneOwnedByAnotherCompany()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis = [CreateProfile(3, "Clone Empresa 8", 8)]
        };
        var service = CreateService(repository, companyId: 7);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetByIdAsync(3));
    }

    [Fact]
    public async Task GetByIdAsync_RejectsLegacyDefaultOwnedByAnotherCompany()
    {
        var repository = new StubPerfilMapeamentoRepository
        {
            Perfis = [CreateProfile(3, "Legado Empresa 8", 8, isDefault: true)]
        };
        var service = CreateService(repository, companyId: 7);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetByIdAsync(3));
    }

    private static PerfilMapeamentoService CreateService(
        StubPerfilMapeamentoRepository repository,
        int companyId) =>
        new(
            repository,
            new StubMessageService(),
            new StubUsuarioAcessoService(new Usuario
            {
                Id = 10,
                FK_IdEmpresa = companyId,
                TipoUsuario = TipoUsuario.Usuario
            }),
            NullLogger<PerfilMapeamentoService>.Instance);

    private static PerfilMapeamento CreateProfile(
        int id,
        string name,
        int? companyId,
        bool isDefault = false) =>
        new()
        {
            Id = id,
            Nome = name,
            FK_IdDocumento = 100,
            FK_IdEmpresa = companyId,
            IsPadrao = isDefault,
            Documento = new Documento { Id = 100 },
            Itens = []
        };
}
