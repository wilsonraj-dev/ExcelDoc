using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelDoc.Server.Tests;

public sealed class MapeamentoTenantTests
{
    [Fact]
    public async Task GetByColecaoAsync_TreatsLegacyCompanyDefaultAsTenantMapping()
    {
        var colecao = new Colecao { Id = 10 };
        var repository = new StubMapeamentoRepository
        {
            Colecao = colecao,
            Mapeamentos =
            [
                CreateMapping(1, colecao, null, isDefault: true),
                CreateMapping(2, colecao, 7, isDefault: true),
                CreateMapping(3, colecao, 8, isDefault: true)
            ]
        };
        var service = CreateService(repository, companyId: 7);

        var result = await service.GetByColecaoAsync(colecao.Id);

        Assert.Equal([1, 2], result.Select(mapping => mapping.Id));
        Assert.True(result.Single(mapping => mapping.Id == 1).IsPadrao);
        Assert.False(result.Single(mapping => mapping.Id == 2).IsPadrao);
    }

    [Fact]
    public async Task GetByIdAsync_RejectsLegacyDefaultOwnedByAnotherCompany()
    {
        var colecao = new Colecao { Id = 10 };
        var repository = new StubMapeamentoRepository
        {
            Colecao = colecao,
            Mapeamentos = [CreateMapping(3, colecao, 8, isDefault: true)]
        };
        var service = CreateService(repository, companyId: 7);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetByIdAsync(3));
    }

    private static MapeamentoService CreateService(StubMapeamentoRepository repository, int companyId) =>
        new(
            repository,
            new StubMessageService(),
            new StubUsuarioAcessoService(new Usuario
            {
                Id = 10,
                FK_IdEmpresa = companyId,
                TipoUsuario = TipoUsuario.Usuario
            }),
            NullLogger<MapeamentoService>.Instance);

    private static Mapeamento CreateMapping(
        int id,
        Colecao colecao,
        int? companyId,
        bool isDefault = false) =>
        new()
        {
            Id = id,
            Nome = $"Mapeamento {id}",
            FK_IdColecao = colecao.Id,
            Colecao = colecao,
            FK_IdEmpresa = companyId,
            IsPadrao = isDefault,
            Campos = []
        };
}
