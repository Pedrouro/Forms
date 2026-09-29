using Forms.API.Enums;

namespace Forms.API.Models
{
    public class FormularioModel
    {
        public int Id { get; set; }
        public required string Titulo { get; set; }
        public string? Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public StatusFormulario Status { get; set; } = StatusFormulario.Rascunho;

        public int CriadorId { get; set; }
        public required UsuarioModel Criador { get; set; }

        public ICollection<PerguntaModel> Perguntas { get; set; } = new List<PerguntaModel>();
    }
}