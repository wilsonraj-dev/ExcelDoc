using ExcelDoc.Server.DTOs.Colecoes;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Logging;

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

        public async Task<IReadOnlyCollection<ColecaoResponseDto>> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(empresaId, false, cancellationToken);
            var colecoes = await _colecaoRepository.GetByEmpresaIdAsync(empresaId, cancellationToken);
            return colecoes.Select(Map).ToList();
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

        private static ColecaoResponseDto Map(Colecao colecao)
        {
            return new ColecaoResponseDto
            {
                Id = colecao.Id,
                NomeColecao = colecao.NomeColecao,
                TipoColecao = colecao.TipoColecao,
                EmpresaId = colecao.FK_IdEmpresa,
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
