using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Tests;

internal sealed class StubMessageService : IMessageService
{
    public string Get(string key) => key;

    public string Get(string key, params object[] arguments) => key;
}

internal sealed class StubUsuarioAcessoService(Usuario usuario) : IUsuarioAcessoService
{
    public Task<Usuario> GetUsuarioAtualAsync(
        bool requerEmpresaVinculada = true,
        CancellationToken cancellationToken = default) => Task.FromResult(usuario);

    public Task<Usuario> ValidarAcessoEmpresaAsync(
        int empresaId,
        bool requerAdministrador,
        CancellationToken cancellationToken = default) => Task.FromResult(usuario);
}

internal sealed class StubPerfilMapeamentoRepository : IPerfilMapeamentoRepository
{
    public IReadOnlyCollection<PerfilMapeamento> Perfis { get; init; } = [];

    public Task<PerfilMapeamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Perfis.SingleOrDefault(perfil => perfil.Id == id));

    public Task<PerfilMapeamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyCollection<PerfilMapeamento>> GetByDocumentoIdAsync(
        int documentoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<PerfilMapeamento>>(
            Perfis.Where(perfil => perfil.FK_IdDocumento == documentoId).ToList());

    public Task<IReadOnlyCollection<DocumentoColecao>> GetColecoesDoDocumentoAsync(
        int documentoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<DocumentoColecao>>([]);

    public Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Mapeamento?>(null);

    public Task<Documento?> GetDocumentoByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Documento?>(null);

    public Task AddAsync(PerfilMapeamento perfil, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public void Remove(PerfilMapeamento perfil) => throw new NotSupportedException();

    public Task RemoveWithOrphanMappingsAsync(
        PerfilMapeamento perfil,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class StubMapeamentoRepository : IMapeamentoRepository
{
    public Colecao? Colecao { get; init; }
    public IReadOnlyCollection<Mapeamento> Mapeamentos { get; init; } = [];

    public Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Colecao?.Id == colecaoId ? Colecao : null);

    public Task<IReadOnlyCollection<Mapeamento>> GetMapeamentosByColecaoIdAsync(
        int colecaoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Mapeamento>>(
            Mapeamentos.Where(mapping => mapping.FK_IdColecao == colecaoId).ToList());

    public Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Mapeamentos.SingleOrDefault(mapping => mapping.Id == id));

    public Task<MapeamentoCampo?> GetCampoByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<MapeamentoCampo?>(null);

    public Task<IReadOnlyCollection<MapeamentoCampo>> GetCamposByMapeamentoIdAsync(
        int mapeamentoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<MapeamentoCampo>>([]);

    public Task<bool> ExistsIndiceNoMapeamentoAsync(
        int mapeamentoId,
        int indiceColuna,
        int? ignoreId = null,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task AddMapeamentoAsync(Mapeamento mapeamento, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddCampoAsync(MapeamentoCampo campo, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public void RemoveMapeamento(Mapeamento mapeamento) => throw new NotSupportedException();

    public void RemoveCampo(MapeamentoCampo campo) => throw new NotSupportedException();

    public Task ReplaceCamposAsync(
        Mapeamento mapeamento,
        IReadOnlyCollection<MapeamentoCampo> campos,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
