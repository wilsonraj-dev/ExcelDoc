using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExcelDoc.Server.Tests;

public sealed class MapeamentoAuthorizationTests
{
    [Fact]
    public async Task GetByColecaoAsync_ReturnsDefaultAndCustomMappings()
    {
        var colecao = new Colecao { Id = 10 };
        var repository = new StubMapeamentoRepository
        {
            Colecao = colecao,
            Mapeamentos =
            [
                CreateMapping(1, colecao, isDefault: true),
                CreateMapping(2, colecao),
                CreateMapping(3, colecao)
            ]
        };
        var service = CreateService(repository);

        var result = await service.GetByColecaoAsync(colecao.Id);

        Assert.Equal([1, 2, 3], result.Select(mapping => mapping.Id));
        Assert.True(result.Single(mapping => mapping.Id == 1).IsPadrao);
        Assert.False(result.Single(mapping => mapping.Id == 2).IsPadrao);
    }

    [Fact]
    public async Task AtualizarAsync_RejectsDefaultMappingForRegularUser()
    {
        var colecao = new Colecao { Id = 10 };
        var repository = new StubMapeamentoRepository
        {
            Colecao = colecao,
            Mapeamentos = [CreateMapping(1, colecao, isDefault: true)]
        };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AtualizarAsync(1, new MapeamentoRequestDto
            {
                Nome = "Novo nome",
                FK_IdColecao = colecao.Id,
                IsPadrao = true
            }));
    }

    private static MapeamentoService CreateService(StubMapeamentoRepository repository) =>
        new(
            repository,
            new StubMessageService(),
            new StubUsuarioAcessoService(new Usuario
            {
                Id = 10,
                TipoUsuario = TipoUsuario.Usuario
            }),
            NullLogger<MapeamentoService>.Instance);

    private static Mapeamento CreateMapping(
        int id,
        Colecao colecao,
        bool isDefault = false) =>
        new()
        {
            Id = id,
            Nome = $"Mapeamento {id}",
            FK_IdColecao = colecao.Id,
            Colecao = colecao,
            IsPadrao = isDefault,
            Campos = []
        };
}
