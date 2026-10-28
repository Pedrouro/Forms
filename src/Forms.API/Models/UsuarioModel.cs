using Forms.API.Enums;

namespace Forms.API.Models
{
    public class UsuarioModel
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public required string Senha { get; set; }
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comum;
    }
}
