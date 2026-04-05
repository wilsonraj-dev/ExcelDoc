using ExcelDoc.Server.DTOs.Empresas;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExcelDoc.Server.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<EmpresaService> _logger;

        public EmpresaService(
            IEmpresaRepository empresaRepository,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<EmpresaService> logger)
        {
            _empresaRepository = empresaRepository;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<EmpresaResponseDto>> GetDisponiveisAsync(CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                var empresas = await _empresaRepository.GetAllAsync(cancellationToken);
                return empresas.Select(Map).ToList();
            }

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                return Array.Empty<EmpresaResponseDto>();
            }

            var empresa = await _empresaRepository.GetByIdAsync(usuario.FK_IdEmpresa.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Empresa do usuário não encontrada.");

            return new[] { Map(empresa) };
        }

        public async Task<EmpresaResponseDto> CriarAsync(EmpresaRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            if (usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem cadastrar empresas.");
            }

            var nomeEmpresa = request.NomeEmpresa.Trim();
            if (string.IsNullOrWhiteSpace(nomeEmpresa))
            {
                throw new InvalidOperationException("Nome da empresa é obrigatório.");
            }

            if (await _empresaRepository.ExistsByNameAsync(nomeEmpresa, cancellationToken))
            {
                throw new InvalidOperationException("Já existe uma empresa cadastrada com este nome.");
            }

            var empresa = new Empresa
            {
                NomeEmpresa = nomeEmpresa
            };

            await _empresaRepository.AddAsync(empresa, cancellationToken);
            await _empresaRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Empresa {EmpresaId} criada pelo usuário {UsuarioId}", empresa.Id, usuario.Id);

            return Map(empresa);
        }

        private static EmpresaResponseDto Map(Empresa empresa)
        {
            return new EmpresaResponseDto
            {
                Id = empresa.Id,
                NomeEmpresa = empresa.NomeEmpresa
            };
        }
    }
}
