using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class ColecaoService : IColecaoService
    {
        private readonly IColecaoRepository _colecaoRepository;
        private readonly IMessageService _messageService;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<ColecaoService> _logger;

        public ColecaoService(IColecaoRepository colecaoRepository, IMessageService messageService, IUsuarioAcessoService usuarioAcessoService, ILogger<ColecaoService> logger)
        {
            _colecaoRepository = colecaoRepository;
            _messageService = messageService;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<ColecaoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var colecoes = await _colecaoRepository.GetAllAsync(cancellationToken);
            return colecoes.Select(Map).ToList();
        }

        public async Task<ColecaoResponseDto> GetByIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            return Map(colecao);
        }

        public async Task<ColecaoResponseDto> CriarAsync(ColecaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var nomeColecao = NormalizeNome(request.NomeColecao);
            var isPadrao = ResolveIsPadrao(usuario, request.IsPadrao);
            var documentoIds = request.DocumentoIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            await ValidarColecaoAsync(nomeColecao, request.TipoColecao, null, documentoIds, cancellationToken);

            var documentos = await _colecaoRepository.GetDocumentosByIdsAsync(documentoIds, cancellationToken);
            EnsureAllDocumentosExist(documentoIds, documentos);

            var colecao = new Colecao
            {
                NomeColecao = nomeColecao,
                Descricao = request.Descricao?.Trim(),
                TipoColecao = request.TipoColecao,
                IsPadrao = isPadrao,
                DocumentoColecoes = documentos.Select(documento => new DocumentoColecao
                {
                    FK_IdDocumento = documento.Id
                }).ToList()
            };

            await _colecaoRepository.AddAsync(colecao, cancellationToken);
            await _colecaoRepository.SaveChangesAsync(cancellationToken);

            return Map(colecao);
        }

        public async Task<ColecaoResponseDto> AtualizarAsync(int colecaoId, ColecaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            EnsureCanEditColecao(usuario, colecao);

            var nomeColecao = NormalizeNome(request.NomeColecao);
            var isPadrao = ResolveIsPadrao(usuario, request.IsPadrao);
            var documentoIds = request.DocumentoIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            await ValidarColecaoAsync(nomeColecao, request.TipoColecao, colecaoId, documentoIds, cancellationToken);

            var documentos = await _colecaoRepository.GetDocumentosByIdsAsync(documentoIds, cancellationToken);
            EnsureAllDocumentosExist(documentoIds, documentos);

            colecao.NomeColecao = nomeColecao;
            colecao.Descricao = request.Descricao?.Trim();
            colecao.TipoColecao = request.TipoColecao;
            colecao.IsPadrao = isPadrao;
            SynchronizeDocumentos(colecao, documentoIds);

            await _colecaoRepository.SaveChangesAsync(cancellationToken);

            return Map(colecao);
        }

        public async Task ExcluirAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(cancellationToken);
            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            EnsureCanEditColecao(usuario, colecao);

            if (colecao.DocumentoColecoes.Any())
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionDeleteLinkedDocuments));
            }

            try
            {
                _colecaoRepository.Remove(colecao);
                await _colecaoRepository.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionDeleteActiveLinks));
            }
        }

        private async Task ValidarColecaoAsync(string nomeColecao, TipoColecao tipoColecao, int? ignoreId, IReadOnlyCollection<int> documentoIds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nomeColecao))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionNameRequired));
            }

            if (nomeColecao.Length > 150)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionNameMaxLength));
            }

            if (await _colecaoRepository.ExistsByNomeAsync(nomeColecao, tipoColecao, ignoreId, cancellationToken))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionAlreadyExists));
            }

            if (documentoIds.Any(x => x <= 0))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.InvalidDocumentLinks));
            }
        }

        private void EnsureAllDocumentosExist(IReadOnlyCollection<int> documentoIds, IReadOnlyCollection<Documento> documentos)
        {
            if (documentoIds.Count != documentos.Count)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.OneOrMoreDocumentsNotFound));
            }
        }

        private static string NormalizeNome(string nomeColecao)
        {
            return nomeColecao.Trim();
        }

        private static void SynchronizeDocumentos(Colecao colecao, IReadOnlyCollection<int> documentoIds)
        {
            var documentoIdsExistentes = colecao.DocumentoColecoes
                .Select(x => x.FK_IdDocumento)
                .ToHashSet();

            var relacoesParaRemover = colecao.DocumentoColecoes
                .Where(x => !documentoIds.Contains(x.FK_IdDocumento))
                .ToList();

            foreach (var relacao in relacoesParaRemover)
            {
                colecao.DocumentoColecoes.Remove(relacao);
            }

            foreach (var documentoId in documentoIds)
            {
                if (documentoIdsExistentes.Contains(documentoId))
                {
                    continue;
                }

                colecao.DocumentoColecoes.Add(new DocumentoColecao
                {
                    FK_IdDocumento = documentoId,
                    FK_IdColecao = colecao.Id
                });
            }
        }

        private bool ResolveIsPadrao(Usuario usuario, bool isPadrao)
        {
            if (isPadrao && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(
                    _messageService.Get(MessageKeys.OnlyAdminsCanChangeSystemCollections));
            }

            return isPadrao;
        }

        private void EnsureCanEditColecao(Usuario usuario, Colecao colecao)
        {
            if (usuario.TipoUsuario != TipoUsuario.Administrador && colecao.IsPadrao)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.OnlyAdminsCanChangeSystemCollections));
            }
        }

        private static ColecaoResponseDto Map(Colecao colecao)
        {
            var campos = ObterCamposDoMapeamentoVisivel(colecao);

            return new ColecaoResponseDto
            {
                Id = colecao.Id,
                NomeColecao = colecao.NomeColecao,
                Descricao = colecao.Descricao,
                TipoColecao = colecao.TipoColecao,
                IsPadrao = colecao.IsPadrao,
                DocumentoIds = colecao.DocumentoColecoes
                    .Select(x => x.FK_IdDocumento)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                Documentos = colecao.DocumentoColecoes
                    .Where(x => x.Documento is not null)
                    .Select(x => new DocumentoResponseDto
                    {
                        Id = x.Documento.Id,
                        NomeDocumento = x.Documento.NomeDocumento,
                        Endpoint = x.Documento.Endpoint
                    })
                    .OrderBy(x => x.NomeDocumento)
                    .ToList(),
                Campos = campos
                    .OrderBy(x => x.IndiceColuna)
                    .Select(x => new MapeamentoCampoResponseDto
                    {
                        Id = x.Id,
                        IndiceColuna = x.IndiceColuna,
                        NomeCampo = x.NomeCampo,
                        DescricaoCampo = x.DescricaoCampo,
                        TipoCampo = x.TipoCampo,
                        Formato = x.Formato,
                        Ativo = x.Ativo
                    })
                    .ToList()
            };
        }

        private static IReadOnlyCollection<MapeamentoCampo> ObterCamposDoMapeamentoVisivel(Colecao colecao)
        {
            var mapeamentoPadrao = colecao.Mapeamentos
                .OrderByDescending(x => x.IsPadrao)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            return mapeamentoPadrao is null ? Array.Empty<MapeamentoCampo>() : mapeamentoPadrao.Campos.ToList();
        }
    }
}
