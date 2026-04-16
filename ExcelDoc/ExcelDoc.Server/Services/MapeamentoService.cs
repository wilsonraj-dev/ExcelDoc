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

        public MapeamentoService(
            IMapeamentoRepository mapeamentoRepository,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<MapeamentoService> logger)
        {
            _mapeamentoRepository = mapeamentoRepository;
            _usuarioAcessoService = usuarioAcessoService;
        }

        public async Task<IReadOnlyCollection<MapeamentoResponseDto>> GetByColecaoAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanAccessColecao(usuario, colecao);

            var campos = await _mapeamentoRepository.GetByColecaoIdAsync(colecaoId, cancellationToken);
            return campos.Select(Map).ToList();
        }

        public async Task<MapeamentoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanAccessColecao(usuario, mapeamento.Colecao);

            return Map(mapeamento);
        }

        public async Task<MapeamentoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(request.FK_IdColecao, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanEditColecao(usuario, colecao);
            await ValidarMapeamentoAsync(request, null, cancellationToken);

            var mapeamento = new MapeamentoCampo
            {
                NomeCampo = request.NomeCampo.Trim(),
                DescricaoCampo = request.DescricaoCampo.Trim(),
                IndiceColuna = request.IndiceColuna,
                TipoCampo = request.TipoCampo,
                Formato = request.TipoCampo == TipoCampo.DateTime ? request.Formato?.Trim() : null,
                FK_IdColecao = request.FK_IdColecao
            };

            await _mapeamentoRepository.AddAsync(mapeamento, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            return Map(mapeamento);
        }

        public async Task<MapeamentoResponseDto> AtualizarAsync(int id, MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditColecao(usuario, mapeamento.Colecao);

            if (mapeamento.FK_IdColecao != request.FK_IdColecao)
            {
                throw new InvalidOperationException("Não é permitido alterar a coleção de um mapeamento existente.");
            }

            await ValidarMapeamentoAsync(request, id, cancellationToken);

            mapeamento.NomeCampo = request.NomeCampo.Trim();
            mapeamento.DescricaoCampo = request.DescricaoCampo.Trim();
            mapeamento.IndiceColuna = request.IndiceColuna;
            mapeamento.TipoCampo = request.TipoCampo;
            mapeamento.Formato = request.TipoCampo == TipoCampo.DateTime ? request.Formato?.Trim() : null;

            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            return Map(mapeamento);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditColecao(usuario, mapeamento.Colecao);

            _mapeamentoRepository.Remove(mapeamento);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidarMapeamentoAsync(MapeamentoRequestDto request, int? ignoreId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.NomeCampo))
            {
                throw new InvalidOperationException("Nome do campo é obrigatório.");
            }

            if (request.IndiceColuna < 1)
            {
                throw new InvalidOperationException("Índice da coluna deve ser um número positivo.");
            }

            if (request.TipoCampo == TipoCampo.DateTime && string.IsNullOrWhiteSpace(request.Formato))
            {
                throw new InvalidOperationException("Formato é obrigatório quando o tipo do campo é DateTime.");
            }

            if (await _mapeamentoRepository.ExistsIndiceNaColecaoAsync(request.FK_IdColecao, request.IndiceColuna, request.NomeCampo, ignoreId, cancellationToken))
            {
                throw new InvalidOperationException($"Já existe um campo com o índice de coluna {request.IndiceColuna} nesta coleção.");
            }
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

        private static void EnsureCanEditColecao(Usuario usuario, Colecao colecao)
        {
            EnsureCanAccessColecao(usuario, colecao);

            if (usuario.TipoUsuario != TipoUsuario.Administrador && !colecao.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem alterar coleções padrão do sistema.");
            }
        }

        private static MapeamentoResponseDto Map(MapeamentoCampo mapeamento)
        {
            return new MapeamentoResponseDto
            {
                Id = mapeamento.Id,
                NomeCampo = mapeamento.NomeCampo,
                DescricaoCampo = mapeamento.DescricaoCampo,
                IndiceColuna = mapeamento.IndiceColuna,
                TipoCampo = mapeamento.TipoCampo,
                Formato = mapeamento.Formato,
                FK_IdColecao = mapeamento.FK_IdColecao
            };
        }

    }
}
