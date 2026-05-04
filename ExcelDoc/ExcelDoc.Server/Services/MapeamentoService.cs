using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class MapeamentoService : IMapeamentoService
    {
        private readonly IMapeamentoRepository _mapeamentoRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<MapeamentoService> _logger;

        public MapeamentoService(
            IMapeamentoRepository mapeamentoRepository,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<MapeamentoService> logger)
        {
            _mapeamentoRepository = mapeamentoRepository;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<MapeamentoResumoResponseDto>> GetByColecaoAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanAccessColecao(usuario, colecao);

            var mapeamentos = await _mapeamentoRepository.GetMapeamentosByColecaoIdAsync(colecaoId, cancellationToken);
            return mapeamentos
                .Where(mapeamento => PodeVisualizarMapeamento(usuario, mapeamento))
                .Select(Map)
                .ToList();
        }

        public async Task<MapeamentoResumoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanAccessMapeamento(usuario, mapeamento);
            return Map(mapeamento);
        }

        public async Task<MapeamentoResumoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(request.FK_IdColecao, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanCreateMapeamento(usuario, request, colecao);

            var mapeamento = new Mapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdColecao = request.FK_IdColecao,
                FK_IdEmpresa = usuario.TipoUsuario == TipoUsuario.Administrador ? request.FK_IdEmpresa : usuario.FK_IdEmpresa,
                IsPadrao = usuario.TipoUsuario == TipoUsuario.Administrador && request.IsPadrao,
                DataCriacao = DateTime.UtcNow,
                Colecao = colecao
            };

            await _mapeamentoRepository.AddMapeamentoAsync(mapeamento, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mapeamento {MapeamentoId} criado para a coleção {ColecaoId} pelo usuário {UsuarioId}", mapeamento.Id, colecao.Id, usuario.Id);

            return Map(mapeamento);
        }

        public async Task<MapeamentoResumoResponseDto> ClonarAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var origem = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanAccessMapeamento(usuario, origem);

            if (!usuario.FK_IdEmpresa.HasValue && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode clonar mapeamentos.");
            }

            var clone = new Mapeamento
            {
                Nome = BuildCloneName(origem.Nome),
                FK_IdColecao = origem.FK_IdColecao,
                FK_IdEmpresa = usuario.TipoUsuario == TipoUsuario.Administrador ? usuario.FK_IdEmpresa : usuario.FK_IdEmpresa,
                IsPadrao = false,
                DataCriacao = DateTime.UtcNow,
                Campos = origem.Campos
                    .OrderBy(campo => campo.IndiceColuna)
                    .Select(campo => new MapeamentoCampo
                    {
                        NomeCampo = campo.NomeCampo,
                        IndiceColuna = campo.IndiceColuna,
                        TipoCampo = campo.TipoCampo,
                        Formato = campo.Formato
                    })
                    .ToList()
            };

            await _mapeamentoRepository.AddMapeamentoAsync(clone, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mapeamento {MapeamentoOrigemId} clonado para {MapeamentoCloneId} pelo usuário {UsuarioId}", origem.Id, clone.Id, usuario.Id);

            return Map(clone);
        }

        public async Task<MapeamentoResumoResponseDto> AtualizarAsync(int id, MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditMapeamento(usuario, mapeamento);

            if (mapeamento.FK_IdColecao != request.FK_IdColecao)
            {
                throw new InvalidOperationException("Não é permitido alterar a coleção do mapeamento.");
            }

            if (mapeamento.IsPadrao && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem alterar mapeamentos padrão.");
            }

            mapeamento.Nome = request.Nome.Trim();

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                mapeamento.FK_IdEmpresa = request.FK_IdEmpresa;
                mapeamento.IsPadrao = request.IsPadrao;
            }

            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
            return Map(mapeamento);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditMapeamento(usuario, mapeamento);

            if (mapeamento.IsPadrao)
            {
                throw new InvalidOperationException("Mapeamentos padrão não podem ser excluídos.");
            }

            _mapeamentoRepository.RemoveMapeamento(mapeamento);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private static string BuildCloneName(string nome)
        {
            return nome.Contains("Cópia", StringComparison.OrdinalIgnoreCase)
                ? nome
                : $"{nome} - Cópia";
        }

        private static bool PodeVisualizarMapeamento(Usuario usuario, Mapeamento mapeamento)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return true;
            }

            if (mapeamento.IsPadrao)
            {
                return true;
            }

            return usuario.FK_IdEmpresa == mapeamento.FK_IdEmpresa;
        }

        private static void EnsureCanAccessColecao(Usuario usuario, Colecao colecao)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return;
            }

            if (!colecao.FK_IdEmpresa.HasValue)
            {
                return;
            }

            if (usuario.FK_IdEmpresa != colecao.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não possui acesso a esta coleção.");
            }
        }

        private static void EnsureCanAccessMapeamento(Usuario usuario, Mapeamento mapeamento)
        {
            EnsureCanAccessColecao(usuario, mapeamento.Colecao);

            if (!PodeVisualizarMapeamento(usuario, mapeamento))
            {
                throw new UnauthorizedAccessException("Usuário não possui acesso a este mapeamento.");
            }
        }

        private static void EnsureCanEditMapeamento(Usuario usuario, Mapeamento mapeamento)
        {
            EnsureCanAccessMapeamento(usuario, mapeamento);

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return;
            }

            if (mapeamento.IsPadrao || mapeamento.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não possui permissão para alterar este mapeamento.");
            }
        }

        private static void EnsureCanCreateMapeamento(Usuario usuario, MapeamentoRequestDto request, Colecao colecao)
        {
            EnsureCanAccessColecao(usuario, colecao);

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return;
            }

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode criar mapeamentos.");
            }

            if (request.IsPadrao)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem criar mapeamentos padrão.");
            }

            if (request.FK_IdEmpresa.HasValue && request.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não pode criar mapeamentos para outra empresa.");
            }
        }

        private static MapeamentoResumoResponseDto Map(Mapeamento mapeamento)
        {
            return new MapeamentoResumoResponseDto
            {
                Id = mapeamento.Id,
                Nome = mapeamento.Nome,
                FK_IdColecao = mapeamento.FK_IdColecao,
                FK_IdEmpresa = mapeamento.FK_IdEmpresa,
                IsPadrao = mapeamento.IsPadrao,
                QuantidadeCampos = mapeamento.Campos.Count
            };
        }
    }
}
