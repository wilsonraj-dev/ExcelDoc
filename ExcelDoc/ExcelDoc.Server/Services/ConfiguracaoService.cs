using ExcelDoc.Server.DTOs.Configuracoes;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class ConfiguracaoService : IConfiguracaoService
    {
        private readonly IConfiguracaoRepository _configuracaoRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly ILogger<ConfiguracaoService> _logger;

        public ConfiguracaoService(
            IConfiguracaoRepository configuracaoRepository,
            IEncryptionService encryptionService,
            IUsuarioAcessoService usuarioAcessoService,
            ILogger<ConfiguracaoService> logger)
        {
            _configuracaoRepository = configuracaoRepository;
            _encryptionService = encryptionService;
            _usuarioAcessoService = usuarioAcessoService;
            _logger = logger;
        }

        public async Task<ConfiguracaoResponseDto> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.ValidarAcessoEmpresaAsync(empresaId, false, cancellationToken);

            var entity = await _configuracaoRepository.GetByEmpresaIdAsync(empresaId, cancellationToken)
                ?? throw new KeyNotFoundException("Configuração da empresa não encontrada.");

            return Map(entity);
        }

        public async Task<ConfiguracaoResponseDto> UpsertAsync(ConfiguracaoRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioAcessoService.ValidarAcessoEmpresaAsync(request.EmpresaId, false, cancellationToken);

            var entity = await _configuracaoRepository.GetByEmpresaIdAsync(request.EmpresaId, cancellationToken);

            if (entity is null)
            {
                entity = new Configuracao
                {
                    FK_IdEmpresa = request.EmpresaId
                };

                await _configuracaoRepository.AddAsync(entity, cancellationToken);
            }

            entity.LinkServiceLayer = request.LinkServiceLayer.Trim();
            entity.Database = request.Database.Trim();
            entity.UsuarioBanco = _encryptionService.Encrypt(request.UsuarioBanco.Trim());
            entity.SenhaBanco = _encryptionService.Encrypt(request.SenhaBanco);
            entity.UsuarioSAP = _encryptionService.Encrypt(request.UsuarioSAP.Trim());
            entity.SenhaSAP = _encryptionService.Encrypt(request.SenhaSAP);

            await _configuracaoRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuração atualizada para empresa {EmpresaId} por usuário {UsuarioId}", request.EmpresaId, usuario.Id);

            return Map(entity);
        }

        private ConfiguracaoResponseDto Map(Configuracao entity)
        {
            return new ConfiguracaoResponseDto
            {
                Id = entity.Id,
                EmpresaId = entity.FK_IdEmpresa,
                LinkServiceLayer = entity.LinkServiceLayer,
                Database = entity.Database,
                UsuarioBanco = _encryptionService.Decrypt(entity.UsuarioBanco),
                SenhaBanco = _encryptionService.Decrypt(entity.SenhaBanco),
                UsuarioSAP = _encryptionService.Decrypt(entity.UsuarioSAP),
                SenhaSAP = _encryptionService.Decrypt(entity.SenhaSAP)
            };
        }
    }
}
