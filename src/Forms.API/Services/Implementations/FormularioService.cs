using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Mappers;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Interfaces;

namespace Forms.API.Services.Implementations
{
    public class FormularioService : IFormularioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IFormularioRepository _formularioRepository;
        private readonly ICurrentUserService _currentUserService;

        public FormularioService(
            IUsuarioRepository usuarioRepository,
            IFormularioRepository formularioRepository,
            ICurrentUserService currentUserService)
        {
            _usuarioRepository = usuarioRepository;
            _formularioRepository = formularioRepository;
            _currentUserService = currentUserService;
        }

        #region Formulário

        public async Task<ResponseDTO<FormularioResponseDTO>> AddFormularioAsync(FormularioDTO dto)
        {
            int usuarioId = _currentUserService.GetCurrentUserId();
            UsuarioModel usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
                ?? throw new NotFoundException($"Usuário com id {usuarioId} não encontrado.");

            if (dto.Perguntas != null && dto.Perguntas.Any())
            {
                foreach (var p in dto.Perguntas)
                {
                    if (!p.Tipo.PermiteAlternativas() && p.Alternativas != null && p.Alternativas.Any())
                        throw new ArgumentException($"Perguntas do tipo '{p.Tipo}' não aceitam alternativas.");

                    if (p.Alternativas != null && p.Alternativas.Any(a => string.IsNullOrWhiteSpace(a.Texto)))
                        throw new ArgumentException("O texto da alternativa não pode ser vazio.");
                }
            }

            FormularioModel formulario = dto.ToModel(usuario);
            await _formularioRepository.AddAsync(formulario);

            return new ResponseDTO<FormularioResponseDTO>
            {
                Status = true,
                Message = "Formulário criado com sucesso.",
                Data = formulario.ToResponseDTO()
            };
        }

        public async Task<FormularioResponseDTO> GetFormularioByIdAsync(int id)
        {
            FormularioModel? formulario = await _formularioRepository.GetByIdAsync(id);

            if (formulario == null || (!UsuarioPodeAcessar(formulario) && formulario.Status != StatusFormulario.Publicado))
                throw new NotFoundException($"Formulário com id {id} não encontrado.");

            return formulario.ToResponseDTO();
        }

        public async Task<IEnumerable<FormularioResponseDTO>> GetAllFormulariosAsync()
        {
            IEnumerable<FormularioModel> formularios = await _formularioRepository.GetAllAsync();
            return formularios.Select(f => f.ToResponseDTO());
        }

        public async Task<ResponseDTO> UpdateFormularioAsync(int id, FormularioDTO dto)
        {
            FormularioModel? formulario = await _formularioRepository.GetByIdAsync(id);

            if (formulario == null)
                throw new NotFoundException($"Formulário com id {id} não encontrado.");

            if (!UsuarioPodeAcessar(formulario))
                throw new ForbiddenException("Você não tem permissão para alterar este formulário.");

            formulario.ApplyUpdate(dto);
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO
            {
                Status = true,
                Message = "Formulário atualizado com sucesso."
            };
        }

        public async Task<ResponseDTO> DeleteFormularioAsync(int id)
        {
            FormularioModel? formulario = await _formularioRepository.GetByIdAsync(id);

            if (formulario == null)
                throw new NotFoundException($"Formulário com id {id} não encontrado.");

            if (!UsuarioPodeAcessar(formulario))
                throw new ForbiddenException("Você não tem permissão para deletar este formulário.");

            await _formularioRepository.DeleteAsync(formulario);

            return new ResponseDTO
            {
                Status = true,
                Message = "Formulário deletado com sucesso."
            };
        }

        #endregion

        #region Métodos Auxiliares

        private bool UsuarioPodeAcessar(FormularioModel formulario)
        {
            int currentUsuarioId = _currentUserService.GetCurrentUserId();
            PerfilUsuario currentPerfil = _currentUserService.GetCurrentUserPerfil();

            bool isDono = formulario.CriadorId == currentUsuarioId;
            bool isAdmin = currentPerfil == PerfilUsuario.Administrador;

            return isDono || isAdmin;
        }

        #endregion
    }
}