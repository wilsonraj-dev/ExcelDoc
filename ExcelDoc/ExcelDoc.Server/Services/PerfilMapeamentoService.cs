using ExcelDoc.Server.DTOs.PerfilMapeamentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class PerfilMapeamentoService : IPerfilMapeamentoService
    {
        private readonly IPerfilMapeamentoRepository _repository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<PerfilMapeamentoService> _logger;

        public PerfilMapeamentoService(
            IPerfilMapeamentoRepository repository,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<PerfilMapeamentoService> logger)
        {
            _repository = repository;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<PerfilMapeamentoResponseDto>> GetByDocumentoAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            var perfis = await _repository.GetByDocumentoIdAsync(documentoId, cancellationToken);
            return perfis
                .Where(p => PodeVisualizar(usuario, p))
                .Select(Map)
                .ToList();
        }

        public async Task<PerfilMapeamentoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var perfil = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento nao encontrado.");

            EnsureCanAccess(usuario, perfil);
            return Map(perfil);
        }

        public async Task<PerfilMapeamentoResponseDto> CriarAsync(PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            _ = await _repository.GetDocumentoByIdAsync(request.FK_IdDocumento, cancellationToken)
                ?? throw new KeyNotFoundException("Documento nao encontrado.");

            EnsureCanCreate(usuario, request);

            await ValidarItensAsync(request.FK_IdDocumento, request.Itens, cancellationToken);

            var perfil = new PerfilMapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdDocumento = request.FK_IdDocumento,
                FK_IdEmpresa = usuario.TipoUsuario == TipoUsuario.Administrador ? request.FK_IdEmpresa : usuario.FK_IdEmpresa,
                IsPadrao = usuario.TipoUsuario == TipoUsuario.Administrador && request.IsPadrao,
                DataCriacao = DateTime.UtcNow,
                Itens = CreatePerfilMapeamentoItems(request.Itens)
            };

            await _repository.AddAsync(perfil, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var created = await _repository.GetByIdAsync(perfil.Id, cancellationToken);
            _logger.LogInformation("PerfilMapeamento {PerfilId} criado pelo usuario {UsuarioId}", perfil.Id, usuario.Id);
            return Map(created!);
        }

        public async Task<PerfilMapeamentoResponseDto> AtualizarAsync(int id, PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var perfil = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento nao encontrado.");

            EnsureCanEdit(usuario, perfil);

            if (perfil.FK_IdDocumento != request.FK_IdDocumento)
            {
                throw new InvalidOperationException("Nao e permitido alterar o documento do perfil.");
            }

            await ValidarItensAsync(request.FK_IdDocumento, request.Itens, cancellationToken);

            perfil.Nome = request.Nome.Trim();

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                perfil.FK_IdEmpresa = request.FK_IdEmpresa;
                perfil.IsPadrao = request.IsPadrao;
            }

            perfil.Itens.Clear();
            foreach (var item in CreatePerfilMapeamentoItems(request.Itens))
            {
                perfil.Itens.Add(item);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            var updated = await _repository.GetByIdAsync(perfil.Id, cancellationToken);
            return Map(updated!);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var perfil = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento nao encontrado.");

            EnsureCanEdit(usuario, perfil);

            _repository.Remove(perfil);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task<PerfilMapeamentoResponseDto> ClonarAsync(int id, ClonePerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var origem = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento nao encontrado.");

            EnsureCanAccess(usuario, origem);

            if (!usuario.FK_IdEmpresa.HasValue && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Usuario sem empresa vinculada nao pode clonar perfis.");
            }

            var clone = new PerfilMapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdDocumento = origem.FK_IdDocumento,
                FK_IdEmpresa = usuario.FK_IdEmpresa,
                IsPadrao = false,
                DataCriacao = DateTime.UtcNow,
                Itens = ClonePerfilMapeamentoItems(origem.Itens)
            };

            await _repository.AddAsync(clone, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var created = await _repository.GetByIdAsync(clone.Id, cancellationToken);
            _logger.LogInformation("PerfilMapeamento {OrigemId} clonado para {CloneId} pelo usuario {UsuarioId}", origem.Id, clone.Id, usuario.Id);
            return Map(created!);
        }

        private async Task ValidarItensAsync(int documentoId, List<PerfilMapeamentoItemRequestDto> itens, CancellationToken cancellationToken)
        {
            if (itens.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma colecao para compor o perfil.");
            }

            var colecaoIds = itens.Select(i => i.FK_IdColecao).ToList();
            if (colecaoIds.Distinct().Count() != colecaoIds.Count)
            {
                throw new InvalidOperationException("Nao e permitido duplicar colecoes dentro do mesmo perfil.");
            }

            var itensPorColecao = itens.ToDictionary(i => i.FK_IdColecao);
            var colecoesDocumento = await _repository.GetColecoesDoDocumentoAsync(documentoId, cancellationToken);
            var colecoesDocumentoIds = colecoesDocumento.Select(c => c.FK_IdColecao).ToHashSet();

            if (colecoesDocumento.Count == 0)
            {
                throw new InvalidOperationException("O documento informado nao possui colecoes vinculadas.");
            }

            if (colecaoIds.Any(id => !colecoesDocumentoIds.Contains(id)))
            {
                throw new InvalidOperationException("O perfil contem colecoes que nao pertencem ao documento selecionado.");
            }

            var headerColecoesIds = colecoesDocumento
                .Where(item => item.Colecao.TipoColecao == TipoColecao.Header)
                .Select(item => item.FK_IdColecao)
                .ToHashSet();

            if (headerColecoesIds.Count == 0)
            {
                throw new InvalidOperationException("O documento deve possuir ao menos uma colecao do tipo cabecalho vinculada.");
            }

            var lineColecoesIds = colecoesDocumento
                .Where(item => item.Colecao.TipoColecao == TipoColecao.Line)
                .Select(item => item.FK_IdColecao)
                .ToHashSet();

            var selectedHeaderIds = colecaoIds.Where(headerColecoesIds.Contains).ToList();
            if (selectedHeaderIds.Count != 1)
            {
                throw new InvalidOperationException("O perfil deve conter exatamente uma colecao do tipo cabecalho.");
            }

            if (itens.Any(item => headerColecoesIds.Contains(item.FK_IdColecao) && item.FK_IdColecaoPai.HasValue))
            {
                throw new InvalidOperationException("Colecoes de cabecalho nao podem ser aninhadas.");
            }

            var selectedLineIds = colecaoIds.Where(lineColecoesIds.Contains).ToList();
            if (lineColecoesIds.Count > 0 && selectedLineIds.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma colecao do tipo line para compor o perfil.");
            }

            var selectedRootLineIds = itens
                .Where(item => lineColecoesIds.Contains(item.FK_IdColecao) && !item.FK_IdColecaoPai.HasValue)
                .Select(item => item.FK_IdColecao)
                .ToList();

            if (lineColecoesIds.Count > 0 && selectedRootLineIds.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma colecao de linha raiz para compor o perfil.");
            }

            foreach (var item in itens.Where(item => item.FK_IdColecaoPai.HasValue))
            {
                var parentColecaoId = item.FK_IdColecaoPai!.Value;

                if (parentColecaoId == item.FK_IdColecao)
                {
                    throw new InvalidOperationException("Uma colecao nao pode ser filha dela mesma.");
                }

                if (!itensPorColecao.ContainsKey(parentColecaoId))
                {
                    throw new InvalidOperationException("Toda colecao filha deve apontar para uma colecao pai selecionada no perfil.");
                }

                if (!lineColecoesIds.Contains(item.FK_IdColecao) || !lineColecoesIds.Contains(parentColecaoId))
                {
                    throw new InvalidOperationException("Somente colecoes do tipo line podem ser aninhadas.");
                }
            }

            foreach (var item in itens)
            {
                var visitedParentIds = new HashSet<int>();
                var current = item;

                while (current.FK_IdColecaoPai.HasValue)
                {
                    var parentColecaoId = current.FK_IdColecaoPai.Value;

                    if (!visitedParentIds.Add(parentColecaoId))
                    {
                        throw new InvalidOperationException("O relacionamento entre colecoes do perfil contem um ciclo.");
                    }

                    current = itensPorColecao[parentColecaoId];
                }
            }

            foreach (var item in itens)
            {
                var mapeamento = await _repository.GetMapeamentoByIdAsync(item.FK_IdMapeamento, cancellationToken)
                    ?? throw new KeyNotFoundException($"Mapeamento {item.FK_IdMapeamento} nao encontrado.");

                if (mapeamento.FK_IdColecao != item.FK_IdColecao)
                {
                    throw new InvalidOperationException($"Mapeamento {item.FK_IdMapeamento} nao pertence a colecao {item.FK_IdColecao}.");
                }
            }
        }

        private static List<PerfilMapeamentoItem> CreatePerfilMapeamentoItems(List<PerfilMapeamentoItemRequestDto> requestItems)
        {
            var items = requestItems
                .Select(item => new PerfilMapeamentoItem
                {
                    FK_IdColecao = item.FK_IdColecao,
                    FK_IdMapeamento = item.FK_IdMapeamento
                })
                .ToList();

            var itemsByColecaoId = items.ToDictionary(item => item.FK_IdColecao);

            foreach (var requestItem in requestItems.Where(item => item.FK_IdColecaoPai.HasValue))
            {
                itemsByColecaoId[requestItem.FK_IdColecao].ItemPai = itemsByColecaoId[requestItem.FK_IdColecaoPai!.Value];
            }

            return items;
        }

        private static List<PerfilMapeamentoItem> ClonePerfilMapeamentoItems(IEnumerable<PerfilMapeamentoItem> sourceItems)
        {
            var sourceList = sourceItems.ToList();
            var clonedBySourceId = sourceList.ToDictionary(
                item => item.Id,
                item => new PerfilMapeamentoItem
                {
                    FK_IdColecao = item.FK_IdColecao,
                    FK_IdMapeamento = item.FK_IdMapeamento
                });

            foreach (var sourceItem in sourceList.Where(item => item.FK_IdPerfilMapeamentoItemPai.HasValue))
            {
                clonedBySourceId[sourceItem.Id].ItemPai = clonedBySourceId[sourceItem.FK_IdPerfilMapeamentoItemPai!.Value];
            }

            return sourceList.Select(item => clonedBySourceId[item.Id]).ToList();
        }

        private static bool PodeVisualizar(Usuario usuario, PerfilMapeamento perfil)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador) return true;
            if (perfil.IsPadrao) return true;
            return usuario.FK_IdEmpresa == perfil.FK_IdEmpresa;
        }

        private static void EnsureCanAccess(Usuario usuario, PerfilMapeamento perfil)
        {
            if (!PodeVisualizar(usuario, perfil))
            {
                throw new UnauthorizedAccessException("Usuario nao possui acesso a este perfil de mapeamento.");
            }
        }

        private static void EnsureCanEdit(Usuario usuario, PerfilMapeamento perfil)
        {
            EnsureCanAccess(usuario, perfil);

            if (usuario.TipoUsuario == TipoUsuario.Administrador) return;

            if (perfil.IsPadrao || perfil.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuario nao possui permissao para alterar este perfil.");
            }
        }

        private static void EnsureCanCreate(Usuario usuario, PerfilMapeamentoRequestDto request)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador) return;

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuario sem empresa vinculada nao pode criar perfis.");
            }

            if (request.IsPadrao)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem criar perfis padrao.");
            }

            if (request.FK_IdEmpresa.HasValue && request.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuario nao pode criar perfis para outra empresa.");
            }
        }

        private static PerfilMapeamentoResponseDto Map(PerfilMapeamento perfil)
        {
            var itens = perfil.Itens.ToList();
            var itensPorId = itens.ToDictionary(item => item.Id);

            return new PerfilMapeamentoResponseDto
            {
                Id = perfil.Id,
                Nome = perfil.Nome,
                FK_IdDocumento = perfil.FK_IdDocumento,
                FK_IdEmpresa = perfil.FK_IdEmpresa,
                IsPadrao = perfil.IsPadrao,
                DataCriacao = perfil.DataCriacao,
                Itens = itens.Select(i =>
                {
                    PerfilMapeamentoItem? itemPai = null;
                    if (i.FK_IdPerfilMapeamentoItemPai.HasValue)
                    {
                        itensPorId.TryGetValue(i.FK_IdPerfilMapeamentoItemPai.Value, out itemPai);
                    }

                    return new PerfilMapeamentoItemResponseDto
                    {
                        Id = i.Id,
                        FK_IdColecao = i.FK_IdColecao,
                        NomeColecao = i.Colecao?.NomeColecao ?? string.Empty,
                        FK_IdMapeamento = i.FK_IdMapeamento,
                        NomeMapeamento = i.Mapeamento?.Nome ?? string.Empty,
                        FK_IdPerfilMapeamentoItemPai = i.FK_IdPerfilMapeamentoItemPai,
                        FK_IdColecaoPai = itemPai?.FK_IdColecao,
                        NomeColecaoPai = itemPai?.Colecao?.NomeColecao
                    };
                }).ToList()
            };
        }
    }
}
