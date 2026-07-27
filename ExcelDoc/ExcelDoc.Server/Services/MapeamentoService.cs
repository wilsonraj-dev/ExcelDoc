using ExcelDoc.Server.DTOs.Mapeamentos;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class MapeamentoService : IMapeamentoService
    {
        private readonly IMapeamentoRepository _mapeamentoRepository;
        private readonly IMessageService _messageService;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<MapeamentoService> _logger;

        public MapeamentoService(
            IMapeamentoRepository mapeamentoRepository,
            IMessageService messageService,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<MapeamentoService> logger)
        {
            _mapeamentoRepository = mapeamentoRepository;
            _messageService = messageService;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<MapeamentoResumoResponseDto>> GetByColecaoAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            var mapeamentos = await _mapeamentoRepository.GetMapeamentosByColecaoIdAsync(colecaoId, cancellationToken);
            return mapeamentos
                .Select(Map)
                .ToList();
        }

        public async Task<MapeamentoResumoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.MappingNotFound));

            return Map(mapeamento);
        }

        public async Task<MapeamentoResumoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(request.FK_IdColecao, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            EnsureCanCreateMapeamento(usuario, request);

            var mapeamento = new Mapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdColecao = request.FK_IdColecao,
                IsPadrao = request.IsPadrao,
                DataCriacao = DateTime.UtcNow,
                Colecao = colecao
            };

            await _mapeamentoRepository.AddMapeamentoAsync(mapeamento, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mapeamento {MapeamentoId} criado para a coleção {ColecaoId} pelo usuário {UsuarioId}", mapeamento.Id, colecao.Id, usuario.Id);

            return Map(mapeamento);
        }

        public async Task<MapeamentoResumoResponseDto> ClonarAsync(int id, CloneMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var origem = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.MappingNotFound));

            var clone = new Mapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdColecao = origem.FK_IdColecao,
                IsPadrao = false,
                DataCriacao = DateTime.UtcNow,
                Campos = origem.Campos
                    .OrderBy(campo => campo.IndiceColuna)
                    .Select(campo => new MapeamentoCampo
                    {
                        NomeCampo = campo.NomeCampo,
                        DescricaoCampo = campo.DescricaoCampo,
                        IndiceColuna = campo.IndiceColuna,
                        TipoCampo = campo.TipoCampo,
                        Formato = campo.Formato,
                        Ativo = campo.Ativo
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
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.MappingNotFound));

            EnsureCanEditMapeamento(usuario, mapeamento);

            if (mapeamento.FK_IdColecao != request.FK_IdColecao)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.MappingCollectionCannotBeChanged));
            }

            mapeamento.Nome = request.Nome.Trim();

            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
            return Map(mapeamento);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetMapeamentoByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.MappingNotFound));

            EnsureCanEditMapeamento(usuario, mapeamento);

            if (mapeamento.IsPadraoGlobal &&
                usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DefaultMappingsCannotBeDeleted));
            }

            _mapeamentoRepository.RemoveMapeamento(mapeamento);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
        }

        private void EnsureCanEditMapeamento(Usuario usuario, Mapeamento mapeamento)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return;
            }

            if (mapeamento.IsPadraoGlobal)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserDoesNotHavePermissionToChangeMapping));
            }
        }

        private void EnsureCanCreateMapeamento(Usuario usuario, MapeamentoRequestDto request)
        {
            if (request.IsPadrao &&
                usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.OnlyAdminsCanCreateDefaultMappings));
            }
        }

        private static MapeamentoResumoResponseDto Map(Mapeamento mapeamento)
        {
            return new MapeamentoResumoResponseDto
            {
                Id = mapeamento.Id,
                Nome = mapeamento.Nome,
                FK_IdColecao = mapeamento.FK_IdColecao,
                IsPadrao = mapeamento.IsPadraoGlobal,
                QuantidadeCampos = mapeamento.Campos.Count
            };
        }
    }
}
