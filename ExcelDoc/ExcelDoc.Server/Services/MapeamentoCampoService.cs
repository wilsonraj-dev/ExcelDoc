using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class MapeamentoCampoService : IMapeamentoCampoService
    {
        private readonly IMapeamentoRepository _mapeamentoRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;

        public MapeamentoCampoService(
            IMapeamentoRepository mapeamentoRepository,
            IUsuarioAcessoService usuarioAcessoService)
        {
            _mapeamentoRepository = mapeamentoRepository;
            _usuarioAcessoService = usuarioAcessoService;
        }

        public async Task<IReadOnlyCollection<MapeamentoCampoResponseDto>> GetByMapeamentoAsync(int mapeamentoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(mapeamentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanAccessMapeamento(usuario, mapeamento);

            var campos = await _mapeamentoRepository.GetCamposByMapeamentoIdAsync(mapeamentoId, cancellationToken);
            return campos.Select(Map).ToList();
        }

        public async Task<MapeamentoCampoResponseDto> CriarAsync(MapeamentoCampoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(request.FK_IdMapeamento, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditMapeamento(usuario, mapeamento);
            await ValidateCampoAsync(request, null, cancellationToken);

            var campo = new MapeamentoCampo
            {
                NomeCampo = request.NomeCampo.Trim(),
                IndiceColuna = request.IndiceColuna,
                TipoCampo = request.TipoCampo,
                Formato = request.TipoCampo == TipoCampo.DateTime ? request.Formato?.Trim() : null,
                FK_IdMapeamento = request.FK_IdMapeamento
            };

            await _mapeamentoRepository.AddCampoAsync(campo, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            return Map(campo);
        }

        public async Task<MapeamentoCampoResponseDto> AtualizarAsync(int id, MapeamentoCampoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var campo = await _mapeamentoRepository.GetCampoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Campo de mapeamento não encontrado.");

            EnsureCanEditMapeamento(usuario, campo.Mapeamento);

            if (campo.FK_IdMapeamento != request.FK_IdMapeamento)
            {
                throw new InvalidOperationException("Não é permitido alterar o mapeamento do campo.");
            }

            await ValidateCampoAsync(request, id, cancellationToken);

            campo.NomeCampo = request.NomeCampo.Trim();
            campo.IndiceColuna = request.IndiceColuna;
            campo.TipoCampo = request.TipoCampo;
            campo.Formato = request.TipoCampo == TipoCampo.DateTime ? request.Formato?.Trim() : null;

            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
            return Map(campo);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var campo = await _mapeamentoRepository.GetCampoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Campo de mapeamento não encontrado.");

            EnsureCanEditMapeamento(usuario, campo.Mapeamento);
            _mapeamentoRepository.RemoveCampo(campo);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidateCampoAsync(MapeamentoCampoRequestDto request, int? ignoreId, CancellationToken cancellationToken)
        {
            if (request.TipoCampo == TipoCampo.DateTime && string.IsNullOrWhiteSpace(request.Formato))
            {
                throw new InvalidOperationException("Formato é obrigatório quando o tipo do campo é DateTime.");
            }

            if (await _mapeamentoRepository.ExistsIndiceNoMapeamentoAsync(request.FK_IdMapeamento, request.IndiceColuna, ignoreId, cancellationToken))
            {
                throw new InvalidOperationException($"Já existe um campo com o índice de coluna {request.IndiceColuna} neste mapeamento.");
            }
        }

        private static void EnsureCanAccessMapeamento(Usuario usuario, Mapeamento mapeamento)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return;
            }

            if (mapeamento.IsPadrao)
            {
                return;
            }

            if (usuario.FK_IdEmpresa != mapeamento.FK_IdEmpresa)
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

            if (mapeamento.IsPadrao || usuario.FK_IdEmpresa != mapeamento.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não possui permissão para alterar este mapeamento.");
            }
        }

        private static MapeamentoCampoResponseDto Map(MapeamentoCampo campo)
        {
            return new MapeamentoCampoResponseDto
            {
                Id = campo.Id,
                NomeCampo = campo.NomeCampo,
                IndiceColuna = campo.IndiceColuna,
                TipoCampo = campo.TipoCampo,
                Formato = campo.Formato,
                FK_IdMapeamento = campo.FK_IdMapeamento
            };
        }
    }
}
