using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IReadOnlyCollection<ColecaoResponseDto>> GetByEmpresaIdAsync(int? empresaId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            var includeAllCompanies = usuario.TipoUsuario == TipoUsuario.Administrador && !empresaId.HasValue;
            var empresaConsulta = includeAllCompanies ? null : ResolveEmpresaId(usuario, empresaId, false);

            var colecoes = await _colecaoRepository.GetByEmpresaIdAsync(empresaConsulta, includeAllCompanies, cancellationToken);
            return colecoes.Select(Map).ToList();
        }

        public async Task<ColecaoResponseDto> GetByIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            EnsureCanAccessColecao(usuario, colecao);

            return Map(colecao);
        }

        public async Task<ColecaoResponseDto> CriarAsync(ColecaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var nomeColecao = NormalizeNome(request.NomeColecao);
            var empresaId = ResolveEmpresaId(usuario, request.FK_IdEmpresa, true);
            var documentoIds = request.DocumentoIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            await ValidarColecaoAsync(nomeColecao, request.TipoColecao, empresaId, null, documentoIds, cancellationToken);

            var documentos = await _colecaoRepository.GetDocumentosByIdsAsync(documentoIds, cancellationToken);
            EnsureAllDocumentosExist(documentoIds, documentos);

            var colecao = new Colecao
            {
                NomeColecao = nomeColecao,
                Descricao = request.Descricao?.Trim(),
                TipoColecao = request.TipoColecao,
                FK_IdEmpresa = empresaId,
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
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.CollectionNotFound));

            EnsureCanEditColecao(usuario, colecao);

            var nomeColecao = NormalizeNome(request.NomeColecao);
            var empresaId = ResolveEmpresaId(usuario, request.FK_IdEmpresa, true);
            var documentoIds = request.DocumentoIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            await ValidarColecaoAsync(nomeColecao, request.TipoColecao, empresaId, colecaoId, documentoIds, cancellationToken);

            var documentos = await _colecaoRepository.GetDocumentosByIdsAsync(documentoIds, cancellationToken);
            EnsureAllDocumentosExist(documentoIds, documentos);

            colecao.NomeColecao = nomeColecao;
            colecao.Descricao = request.Descricao?.Trim();
            colecao.TipoColecao = request.TipoColecao;
            colecao.FK_IdEmpresa = empresaId;
            SynchronizeDocumentos(colecao, documentoIds);

            await _colecaoRepository.SaveChangesAsync(cancellationToken);

            return Map(colecao);
        }

        public async Task ExcluirAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
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
            catch (DbUpdateException)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionDeleteActiveLinks));
            }
        }

        private async Task ValidarColecaoAsync(string nomeColecao, TipoColecao tipoColecao, int? empresaId, int? ignoreId, IReadOnlyCollection<int> documentoIds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nomeColecao))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionNameRequired));
            }

            if (nomeColecao.Length > 150)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.CollectionNameMaxLength));
            }

            if (await _colecaoRepository.ExistsByNomeAsync(nomeColecao, tipoColecao, empresaId, ignoreId, cancellationToken))
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

        private int? ResolveEmpresaId(Usuario usuario, int? empresaId, bool allowGlobal)
        {
            if (empresaId.HasValue)
            {
                if (usuario.TipoUsuario != TipoUsuario.Administrador && usuario.FK_IdEmpresa != empresaId)
                {
                    throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserDoesNotHaveAccessToCompany));
                }

                return empresaId;
            }

            if (allowGlobal && usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return null;
            }

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserWithoutCompanyCannotExecuteAction));
            }

            return usuario.FK_IdEmpresa;
        }

        private void EnsureCanAccessColecao(Usuario usuario, Colecao colecao)
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
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserDoesNotHaveAccessToCollection));
            }
        }

        private void EnsureCanEditColecao(Usuario usuario, Colecao colecao)
        {
            EnsureCanAccessColecao(usuario, colecao);

            if (usuario.TipoUsuario != TipoUsuario.Administrador && !colecao.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.OnlyAdminsCanChangeSystemCollections));
            }
        }

        private static ColecaoResponseDto Map(Colecao colecao)
        {
            var campos = ObterCamposDoMapeamentoPadrao(colecao);

            return new ColecaoResponseDto
            {
                Id = colecao.Id,
                NomeColecao = colecao.NomeColecao,
                Descricao = colecao.Descricao,
                TipoColecao = colecao.TipoColecao,
                EmpresaId = colecao.FK_IdEmpresa,
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
                        Formato = x.Formato
                    })
                    .ToList()
            };
        }

        private static IReadOnlyCollection<MapeamentoCampo> ObterCamposDoMapeamentoPadrao(Colecao colecao)
        {
            var mapeamentoPadrao = colecao.Mapeamentos.FirstOrDefault(x => x.IsPadrao)
                ?? colecao.Mapeamentos.OrderBy(x => x.Id).FirstOrDefault();

            return mapeamentoPadrao is null ? Array.Empty<MapeamentoCampo>() : mapeamentoPadrao.Campos.ToList();
        }
    }
}
