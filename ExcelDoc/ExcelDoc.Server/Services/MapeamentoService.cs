using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.DTOs.Documentos;
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

            EnsureCanAccessColecao(usuario, mapeamento.Mapeamento.Colecao);

            return Map(mapeamento);
        }

        public async Task<MapeamentoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var colecao = await _mapeamentoRepository.GetColecaoByIdAsync(request.FK_IdColecao, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            EnsureCanEditColecao(usuario, colecao);
            var mapeamentoPadrao = await ObterOuCriarMapeamentoPadraoAsync(colecao, cancellationToken);
            await ValidarMapeamentoAsync(request, mapeamentoPadrao.Id, null, cancellationToken);

            var campo = new MapeamentoCampo
            {
                NomeCampo = request.NomeCampo.Trim(),
                DescricaoCampo = request.DescricaoCampo.Trim(),
                IndiceColuna = request.IndiceColuna,
                TipoCampo = request.TipoCampo,
                Formato = request.TipoCampo == TipoCampo.DateTime ? request.Formato?.Trim() : null,
                FK_IdMapeamento = mapeamentoPadrao.Id
            };

            await _mapeamentoRepository.AddAsync(campo, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            campo.Mapeamento = mapeamentoPadrao;

            return Map(campo);
        }

        public async Task<MapeamentoResponseDto> AtualizarAsync(int id, MapeamentoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);
            var mapeamento = await _mapeamentoRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("Mapeamento não encontrado.");

            EnsureCanEditColecao(usuario, mapeamento.Mapeamento.Colecao);

            if (mapeamento.Mapeamento.FK_IdColecao != request.FK_IdColecao)
            {
                throw new InvalidOperationException("Não é permitido alterar a coleção de um mapeamento existente.");
            }

            await ValidarMapeamentoAsync(request, mapeamento.FK_IdMapeamento, id, cancellationToken);

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

            EnsureCanEditColecao(usuario, mapeamento.Mapeamento.Colecao);

            _mapeamentoRepository.Remove(mapeamento);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task<ColecaoResponseDto> ClonePadraoAsync(CloneColecaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var colecaoPadrao = await _mapeamentoRepository.GetColecaoByIdWithMappingsAsync(request.ColecaoPadraoId, cancellationToken)
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
                Mapeamentos = colecaoPadrao.Mapeamentos.Select(x => new Mapeamento
                {
                    Nome = x.Nome,
                    FK_IdEmpresa = request.EmpresaId,
                    IsPadrao = x.IsPadrao,
                    DataCriacao = DateTime.UtcNow,
                    Campos = x.Campos.Select(campo => new MapeamentoCampo
                    {
                        IndiceColuna = campo.IndiceColuna,
                        NomeCampo = campo.NomeCampo,
                        DescricaoCampo = campo.DescricaoCampo,
                        TipoCampo = campo.TipoCampo,
                        Formato = campo.Formato
                    }).ToList()
                }).ToList()
            };

            await _mapeamentoRepository.AddColecaoAsync(novaColecao, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Coleção padrão {ColecaoPadraoId} clonada para empresa {EmpresaId} pelo usuário {UsuarioId}", request.ColecaoPadraoId, request.EmpresaId, usuario.Id);

            return MapColecao(novaColecao);
        }

        public async Task<ColecaoResponseDto> AtualizarMapeamentosAsync(int colecaoId, AtualizarMapeamentosRequestDto request, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var colecao = await _mapeamentoRepository.GetColecaoByIdWithMappingsAsync(colecaoId, cancellationToken)
                ?? throw new KeyNotFoundException("Coleção não encontrada.");

            if (colecao.FK_IdEmpresa != request.EmpresaId)
            {
                throw new InvalidOperationException("Apenas coleções customizadas da empresa podem ser alteradas.");
            }

            var mapeamentoPadrao = ObterOuCriarMapeamentoPadrao(colecao);
            mapeamentoPadrao.Campos.Clear();

            foreach (var campo in request.Campos.OrderBy(x => x.IndiceColuna))
            {
                mapeamentoPadrao.Campos.Add(new MapeamentoCampo
                {
                    IndiceColuna = campo.IndiceColuna,
                    NomeCampo = campo.NomeCampo.Trim(),
                    DescricaoCampo = campo.DescricaoCampo.Trim(),
                    TipoCampo = campo.TipoCampo,
                    Formato = campo.Formato?.Trim()
                });
            }

            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mapeamentos da coleção {ColecaoId} atualizados para empresa {EmpresaId}", colecaoId, request.EmpresaId);

            return MapColecao(colecao);
        }

        private async Task ValidarMapeamentoAsync(MapeamentoRequestDto request, int mapeamentoId, int? ignoreId, CancellationToken cancellationToken)
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

            if (await _mapeamentoRepository.ExistsIndiceNoMapeamentoAsync(mapeamentoId, request.IndiceColuna, ignoreId, cancellationToken))
            {
                throw new InvalidOperationException($"Já existe um campo com o índice de coluna {request.IndiceColuna} neste mapeamento.");
            }
        }

        private async Task<Mapeamento> ObterOuCriarMapeamentoPadraoAsync(Colecao colecao, CancellationToken cancellationToken)
        {
            var mapeamentoPadrao = await _mapeamentoRepository.GetMapeamentoPadraoByColecaoIdAsync(colecao.Id, cancellationToken);
            if (mapeamentoPadrao is not null)
            {
                return mapeamentoPadrao;
            }

            mapeamentoPadrao = new Mapeamento
            {
                Nome = $"Mapeamento padrão - {colecao.NomeColecao}",
                FK_IdColecao = colecao.Id,
                FK_IdEmpresa = colecao.FK_IdEmpresa,
                IsPadrao = true,
                DataCriacao = DateTime.UtcNow,
                Colecao = colecao
            };

            await _mapeamentoRepository.AddMapeamentoAsync(mapeamentoPadrao, cancellationToken);
            await _mapeamentoRepository.SaveChangesAsync(cancellationToken);

            return mapeamentoPadrao;
        }

        private static Mapeamento ObterOuCriarMapeamentoPadrao(Colecao colecao)
        {
            var mapeamentoPadrao = colecao.Mapeamentos.FirstOrDefault(x => x.IsPadrao);
            if (mapeamentoPadrao is not null)
            {
                return mapeamentoPadrao;
            }

            mapeamentoPadrao = new Mapeamento
            {
                Nome = $"Mapeamento padrão - {colecao.NomeColecao}",
                FK_IdEmpresa = colecao.FK_IdEmpresa,
                IsPadrao = true,
                DataCriacao = DateTime.UtcNow
            };

            colecao.Mapeamentos.Add(mapeamentoPadrao);
            return mapeamentoPadrao;
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
                FK_IdColecao = mapeamento.Mapeamento.FK_IdColecao,
                FK_IdMapeamento = mapeamento.FK_IdMapeamento,
                NomeMapeamento = mapeamento.Mapeamento.Nome
            };
        }

        private static ColecaoResponseDto MapColecao(Colecao colecao)
        {
            var campos = ObterCamposDoMapeamentoPadrao(colecao);

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
                        Id = x.Documento!.Id,
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
