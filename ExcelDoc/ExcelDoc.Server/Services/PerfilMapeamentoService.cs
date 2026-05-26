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
                ?? throw new KeyNotFoundException("Perfil de mapeamento não encontrado.");

            EnsureCanAccess(usuario, perfil);
            return Map(perfil);
        }

        public async Task<PerfilMapeamentoResponseDto> CriarAsync(PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            var documento = await _repository.GetDocumentoByIdAsync(request.FK_IdDocumento, cancellationToken)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            EnsureCanCreate(usuario, request);

            await ValidarItensAsync(request.FK_IdDocumento, request.Itens, cancellationToken);

            var perfil = new PerfilMapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdDocumento = request.FK_IdDocumento,
                FK_IdEmpresa = usuario.TipoUsuario == TipoUsuario.Administrador ? request.FK_IdEmpresa : usuario.FK_IdEmpresa,
                IsPadrao = usuario.TipoUsuario == TipoUsuario.Administrador && request.IsPadrao,
                DataCriacao = DateTime.UtcNow,
                Itens = request.Itens.Select(i => new PerfilMapeamentoItem
                {
                    FK_IdColecao = i.FK_IdColecao,
                    FK_IdMapeamento = i.FK_IdMapeamento
                }).ToList()
            };

            await _repository.AddAsync(perfil, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var created = await _repository.GetByIdAsync(perfil.Id, cancellationToken);
            _logger.LogInformation("PerfilMapeamento {PerfilId} criado pelo usuário {UsuarioId}", perfil.Id, usuario.Id);
            return Map(created!);
        }

        public async Task<PerfilMapeamentoResponseDto> AtualizarAsync(int id, PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var perfil = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento não encontrado.");

            EnsureCanEdit(usuario, perfil);

            if (perfil.FK_IdDocumento != request.FK_IdDocumento)
            {
                throw new InvalidOperationException("Não é permitido alterar o documento do perfil.");
            }

            await ValidarItensAsync(request.FK_IdDocumento, request.Itens, cancellationToken);

            perfil.Nome = request.Nome.Trim();

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                perfil.FK_IdEmpresa = request.FK_IdEmpresa;
                perfil.IsPadrao = request.IsPadrao;
            }

            // Remove itens antigos e adiciona novos
            perfil.Itens.Clear();
            foreach (var item in request.Itens)
            {
                perfil.Itens.Add(new PerfilMapeamentoItem
                {
                    FK_IdColecao = item.FK_IdColecao,
                    FK_IdMapeamento = item.FK_IdMapeamento
                });
            }

            await _repository.SaveChangesAsync(cancellationToken);

            var updated = await _repository.GetByIdAsync(perfil.Id, cancellationToken);
            return Map(updated!);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var perfil = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento não encontrado.");

            EnsureCanEdit(usuario, perfil);

            _repository.Remove(perfil);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task<PerfilMapeamentoResponseDto> ClonarAsync(int id, ClonePerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var origem = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Perfil de mapeamento não encontrado.");

            EnsureCanAccess(usuario, origem);

            if (!usuario.FK_IdEmpresa.HasValue && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode clonar perfis.");
            }

            var clone = new PerfilMapeamento
            {
                Nome = request.Nome.Trim(),
                FK_IdDocumento = origem.FK_IdDocumento,
                FK_IdEmpresa = usuario.FK_IdEmpresa,
                IsPadrao = false,
                DataCriacao = DateTime.UtcNow,
                Itens = origem.Itens.Select(i => new PerfilMapeamentoItem
                {
                    FK_IdColecao = i.FK_IdColecao,
                    FK_IdMapeamento = i.FK_IdMapeamento
                }).ToList()
            };

            await _repository.AddAsync(clone, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var created = await _repository.GetByIdAsync(clone.Id, cancellationToken);
            _logger.LogInformation("PerfilMapeamento {OrigemId} clonado para {CloneId} pelo usuário {UsuarioId}", origem.Id, clone.Id, usuario.Id);
            return Map(created!);
        }

        private async Task ValidarItensAsync(int documentoId, List<PerfilMapeamentoItemRequestDto> itens, CancellationToken cancellationToken)
        {
            if (itens.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma coleção para compor o perfil.");
            }

            // Verificar duplicidade de coleção
            var colecaoIds = itens.Select(i => i.FK_IdColecao).ToList();
            if (colecaoIds.Distinct().Count() != colecaoIds.Count)
            {
                throw new InvalidOperationException("Não é permitido duplicar coleções dentro do mesmo perfil.");
            }

            // Verificar que as coleções selecionadas pertencem ao documento e respeitam a regra por tipo
            var colecoesDocumento = await _repository.GetColecoesDoDocumentoAsync(documentoId, cancellationToken);
            var colecoesDocumentoIds = colecoesDocumento.Select(c => c.FK_IdColecao).ToHashSet();

            if (colecoesDocumento.Count == 0)
            {
                throw new InvalidOperationException("O documento informado não possui coleções vinculadas.");
            }

            if (colecaoIds.Any(id => !colecoesDocumentoIds.Contains(id)))
            {
                throw new InvalidOperationException("O perfil contém coleções que não pertencem ao documento selecionado.");
            }

            var headerColecoesIds = colecoesDocumento
                .Where(item => item.Colecao.TipoColecao == TipoColecao.Header)
                .Select(item => item.FK_IdColecao)
                .ToHashSet();

            if (headerColecoesIds.Count == 0)
            {
                throw new InvalidOperationException("O documento deve possuir ao menos uma coleção do tipo cabeçalho vinculada.");
            }

            var lineColecoesIds = colecoesDocumento
                .Where(item => item.Colecao.TipoColecao == TipoColecao.Line)
                .Select(item => item.FK_IdColecao)
                .ToHashSet();

            var selectedHeaderIds = colecaoIds.Where(headerColecoesIds.Contains).ToList();
            if (selectedHeaderIds.Count != 1)
            {
                throw new InvalidOperationException("O perfil deve conter exatamente uma coleção do tipo cabeçalho.");
            }

            var selectedLineIds = colecaoIds.Where(lineColecoesIds.Contains).ToList();
            if (lineColecoesIds.Count > 0 && selectedLineIds.Count == 0)
            {
                throw new InvalidOperationException("Selecione ao menos uma coleção do tipo line para compor o perfil.");
            }

            // Verificar que cada mapeamento pertence à coleção informada
            foreach (var item in itens)
            {
                var mapeamento = await _repository.GetMapeamentoByIdAsync(item.FK_IdMapeamento, cancellationToken)
                    ?? throw new KeyNotFoundException($"Mapeamento {item.FK_IdMapeamento} não encontrado.");

                if (mapeamento.FK_IdColecao != item.FK_IdColecao)
                {
                    throw new InvalidOperationException($"Mapeamento {item.FK_IdMapeamento} não pertence à coleção {item.FK_IdColecao}.");
                }
            }
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
                throw new UnauthorizedAccessException("Usuário não possui acesso a este perfil de mapeamento.");
            }
        }

        private static void EnsureCanEdit(Usuario usuario, PerfilMapeamento perfil)
        {
            EnsureCanAccess(usuario, perfil);

            if (usuario.TipoUsuario == TipoUsuario.Administrador) return;

            if (perfil.IsPadrao || perfil.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não possui permissão para alterar este perfil.");
            }
        }

        private static void EnsureCanCreate(Usuario usuario, PerfilMapeamentoRequestDto request)
        {
            if (usuario.TipoUsuario == TipoUsuario.Administrador) return;

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode criar perfis.");
            }

            if (request.IsPadrao)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem criar perfis padrão.");
            }

            if (request.FK_IdEmpresa.HasValue && request.FK_IdEmpresa != usuario.FK_IdEmpresa)
            {
                throw new UnauthorizedAccessException("Usuário não pode criar perfis para outra empresa.");
            }
        }

        private static PerfilMapeamentoResponseDto Map(PerfilMapeamento perfil)
        {
            return new PerfilMapeamentoResponseDto
            {
                Id = perfil.Id,
                Nome = perfil.Nome,
                FK_IdDocumento = perfil.FK_IdDocumento,
                FK_IdEmpresa = perfil.FK_IdEmpresa,
                IsPadrao = perfil.IsPadrao,
                DataCriacao = perfil.DataCriacao,
                Itens = perfil.Itens.Select(i => new PerfilMapeamentoItemResponseDto
                {
                    Id = i.Id,
                    FK_IdColecao = i.FK_IdColecao,
                    NomeColecao = i.Colecao?.NomeColecao ?? string.Empty,
                    FK_IdMapeamento = i.FK_IdMapeamento,
                    NomeMapeamento = i.Mapeamento?.Nome ?? string.Empty
                }).ToList()
            };
        }
    }
}
