using Forms.API.Enums;

namespace Forms.API.DTOs
{
    public class UsuarioDTO
    {
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public required string Senha { get; set; }
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comum;
    }
}
