using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Services
{
    public class DocumentoService : IDocumentoService
    {
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;

        public DocumentoService(IDocumentoRepository documentoRepository, IUsuarioAcessoService usuarioAcessoService)
        {
            _documentoRepository = documentoRepository;
            _usuarioAcessoService = usuarioAcessoService;
        }

        public async Task<IReadOnlyCollection<DocumentoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(true, cancellationToken);

            var documentos = await _documentoRepository.GetAllAsync(cancellationToken);

            return documentos
                .Select(Map)
                .ToList();
        }

        public async Task<DocumentoResponseDto> GetByIdAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(true, cancellationToken);

            var documento = await _documentoRepository.GetByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            return Map(documento);
        }

        public async Task<DocumentoResponseDto> CriarAsync(DocumentoRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var nomeDocumento = request.NomeDocumento.Trim();
            var endpoint = request.Endpoint.Trim();

            ValidarCampos(nomeDocumento, endpoint);

            if (await _documentoRepository.ExistsByNomeOrEndpointAsync(nomeDocumento, endpoint, null, cancellationToken))
            {
                throw new InvalidOperationException("Já existe um documento com o mesmo nome ou endpoint.");
            }

            var documento = new Documento
            {
                NomeDocumento = nomeDocumento,
                Endpoint = endpoint
            };

            await _documentoRepository.AddAsync(documento, cancellationToken);
            await _documentoRepository.SaveChangesAsync(cancellationToken);

            return Map(documento);
        }

        public async Task ExcluirAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var documento = await _documentoRepository.GetTrackedByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            try
            {
                _documentoRepository.Remove(documento);
                await _documentoRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Não foi possível excluir o documento porque ele está vinculado a outros registros.");
            }
        }

        public async Task<DocumentoResponseDto> AtualizarAsync(int documentoId, DocumentoRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var documento = await _documentoRepository.GetTrackedByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            var nomeDocumento = request.NomeDocumento.Trim();
            var endpoint = request.Endpoint.Trim();

            ValidarCampos(nomeDocumento, endpoint);

            if (await _documentoRepository.ExistsByNomeOrEndpointAsync(nomeDocumento, endpoint, documentoId, cancellationToken))
            {
                throw new InvalidOperationException("Já existe um documento com o mesmo nome ou endpoint.");
            }

            documento.NomeDocumento = nomeDocumento;
            documento.Endpoint = endpoint;

            await _documentoRepository.SaveChangesAsync(cancellationToken);

            return Map(documento);
        }

        private async Task ValidarAdministradorAsync(CancellationToken cancellationToken)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            if (usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem alterar documentos.");
            }
        }

        private static void ValidarCampos(string nomeDocumento, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(nomeDocumento))
            {
                throw new InvalidOperationException("Nome do documento é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("Endpoint do documento é obrigatório.");
            }
        }

        private static DocumentoResponseDto Map(Documento documento)
        {
            return new DocumentoResponseDto
            {
                Id = documento.Id,
                NomeDocumento = documento.NomeDocumento,
                Endpoint = documento.Endpoint
            };
        }
    }
}
