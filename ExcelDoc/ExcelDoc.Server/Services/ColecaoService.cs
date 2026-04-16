using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Services
{
    public class ColecaoService : IColecaoService
    {
        private readonly IColecaoRepository _colecaoRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<ColecaoService> _logger;

        public ColecaoService(IColecaoRepository colecaoRepository, IUsuarioAcessoService usuarioAcessoService, ILogger<ColecaoService> logger)
        {
            _colecaoRepository = colecaoRepository;
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
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

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
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

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
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanEditColecao(usuario, colecao);

            if (colecao.DocumentoColecoes.Any())
            {
                throw new InvalidOperationException("Não é possível excluir a coleção porque ela está vinculada a documentos.");
            }

            try
            {
                _colecaoRepository.Remove(colecao);
                await _colecaoRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Não foi possível excluir a coleção porque ela possui vínculos ativos.");
            }
        }

        public async Task<ColecaoResponseDto> ClonePadraoAsync(CloneColecaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var colecaoPadrao = await _colecaoRepository.GetByIdWithMappingsAsync(request.ColecaoPadraoId, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção padrão não encontrada.");

            if (colecaoPadrao.FK_IdEmpresa.HasValue)
            {
                throw new InvalidOperationException("Somente coleções padrão do sistema podem ser clonadas.");
            }

            var novaColecao = new Colecao
            {
                NomeColecao = request.NomeColecao.Trim(),
                TipoColecao = colecaoPadrao.TipoColecao,
                FK_IdEmpresa = request.EmpresaId,
                MapeamentoCampos = colecaoPadrao.MapeamentoCampos.Select(x => new MapeamentoCampo
                {
                    IndiceColuna = x.IndiceColuna,
                    NomeCampo = x.NomeCampo,
                    DescricaoCampo = x.DescricaoCampo,
                    TipoCampo = x.TipoCampo,
                    Formato = x.Formato
                }).ToList()
            };

            await _colecaoRepository.AddAsync(novaColecao, cancellationToken);
            await _colecaoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Coleção padrão {ColecaoPadraoId} clonada para empresa {EmpresaId} pelo usuário {UsuarioId}", request.ColecaoPadraoId, request.EmpresaId, usuario.Id);

            return Map(novaColecao);
        }

        public async Task<ColecaoResponseDto> AtualizarMapeamentosAsync(int colecaoId, AtualizarMapeamentosRequestDto request, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var colecao = await _colecaoRepository.GetByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            if (colecao.FK_IdEmpresa != request.EmpresaId)
            {
                throw new InvalidOperationException("Apenas coleções customizadas da empresa podem ser alteradas.");
            }

            colecao.MapeamentoCampos.Clear();

            foreach (var campo in request.Campos.OrderBy(x => x.IndiceColuna))
            {
                colecao.MapeamentoCampos.Add(new MapeamentoCampo
                {
                    IndiceColuna = campo.IndiceColuna,
                    NomeCampo = campo.NomeCampo.Trim(),
                    DescricaoCampo = campo.DescricaoCampo.Trim(),
                    TipoCampo = campo.TipoCampo,
                    Formato = campo.Formato?.Trim()
                });
            }

            await _colecaoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mapeamentos da coleção {ColecaoId} atualizados para empresa {EmpresaId}", colecaoId, request.EmpresaId);

            return Map(colecao);
        }

        private async Task ValidarColecaoAsync(string nomeColecao, TipoColecao tipoColecao, int? empresaId, int? ignoreId, IReadOnlyCollection<int> documentoIds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nomeColecao))
            {
                throw new InvalidOperationException("Nome da coleção é obrigatório.");
            }

            if (nomeColecao.Length > 150)
            {
                throw new InvalidOperationException("Nome da coleção deve ter no máximo 150 caracteres.");
            }

            if (await _colecaoRepository.ExistsByNomeAsync(nomeColecao, tipoColecao, empresaId, ignoreId, cancellationToken))
            {
                throw new InvalidOperationException("Já existe uma coleção com o mesmo nome e tipo no escopo informado.");
            }

            if (documentoIds.Any(x => x <= 0))
            {
                throw new InvalidOperationException("Os documentos informados para vínculo são inválidos.");
            }
        }

        private static void EnsureAllDocumentosExist(IReadOnlyCollection<int> documentoIds, IReadOnlyCollection<Documento> documentos)
        {
            if (documentoIds.Count != documentos.Count)
            {
                throw new InvalidOperationException("Um ou mais documentos informados não foram encontrados.");
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

        private static int? ResolveEmpresaId(Usuario usuario, int? empresaId, bool allowGlobal)
        {
            if (empresaId.HasValue)
            {
                if (usuario.TipoUsuario != TipoUsuario.Administrador && usuario.FK_IdEmpresa != empresaId)
                {
                    throw new UnauthorizedAccessException("Usuário não possui acesso à empresa informada.");
                }

                return empresaId;
            }

            if (allowGlobal && usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                return null;
            }

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode executar esta ação.");
            }

            return usuario.FK_IdEmpresa;
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

        private static ColecaoResponseDto Map(Colecao colecao)
        {
            return new ColecaoResponseDto
            {
                Id = colecao.Id,
                NomeColecao = colecao.NomeColecao,
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
                Campos = colecao.MapeamentoCampos
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
    }
}
