using BCrypt.Net;
using System.Security.Claims;
using System.Net.Mail;
using Forms.API.Enums;
using Forms.API.DTOs;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Interfaces;

namespace Forms.API.Services.Implementations
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _UsuarioRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioService(IUsuarioRepository usuarioRepository, IHttpContextAccessor httpContextAccessor)
        {
            _UsuarioRepository = usuarioRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseDTO> AddUsuario(UsuarioDTO usuario)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);

            if (!EmailValido(usuario.Email))
                throw new ArgumentException("Email inválido.");

            if (EmailJaCadastrado(usuario.Email))
                throw new ArgumentException("Email já cadastrado.");

            UsuarioModel user = new UsuarioModel
            {
                Nome = usuario.Nome,
                Email = usuario.Email,
                Senha = hash,
                Perfil = PerfilUsuario.Comum
            };

            await _UsuarioRepository.AddAsync(user);

            return new ResponseDTO
            {
                Status = true,
                Message = "Usuário adicionado com sucesso."
            };
        }

        public async Task<ResponseDTO> DeleteUsuario(int id)
        {
            await _UsuarioRepository.DeleteAsync(id);

            return new ResponseDTO
            {
                Status = true,
                Message = "Usuário deletado com sucesso."
            };
        }

        public async Task<IEnumerable<UsuarioModel>> GetAllUsuarios()
        {
            return await _UsuarioRepository.GetAllAsync();
        }

        public async Task<UsuarioModel> GetUsuarioById(int id)
        {
            if (!VerificarUsuario(id) && !VerificarPerfilAdmin())
                throw new KeyNotFoundException("Objeto não encontrado.");

            return await _UsuarioRepository.GetByIdAsync(id);
        }

        public async Task<ResponseDTO> UpdateUsuario(UsuarioDTO usuario, int id)
        {
            UsuarioModel usuarioAtualizado = await _UsuarioRepository.GetByIdAsync(id);

            bool senhaAlterada = !BCrypt.Net.BCrypt.Verify(usuario.Senha, usuarioAtualizado.Senha);

            if (senhaAlterada)
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);
                usuarioAtualizado.Senha = hash;
            }

            usuarioAtualizado.Nome = usuario.Nome;

            if (!EmailValido(usuario.Email))
                throw new ArgumentException("Email inválido.");

            usuarioAtualizado.Email = usuario.Email;
            usuarioAtualizado.Perfil = usuario.Perfil;

            await _UsuarioRepository.UpdateAsync(usuarioAtualizado);

            return new ResponseDTO
            {
                Status = true,
                Message = "Usuário atualizado com sucesso."
            };
        }

        private bool VerificarUsuario(int id)
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.Parse(claim.Value) == id)
                return true;

            return false;
        }

        private bool VerificarPerfilAdmin()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role);

            if (claim != null && claim.Value == PerfilUsuario.Administrador.ToString())
                return true;

            return false;
        }

        private static bool EmailValido(string email)
        {
            try
            {
                var endereco = new MailAddress(email);
                return endereco.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool EmailJaCadastrado(string email)
        {
            UsuarioModel? usuario = _UsuarioRepository.GetByEmailAsync(email).Result;
            return usuario != null;
        }
    }
}
