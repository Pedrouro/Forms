using Forms.API.Enums;

namespace Forms.API.Models
{
    public class PerguntaModel
    {
        public int Id { get; set; }
        public required string Enunciado { get; set; }
        public required TipoPergunta Tipo { get; set; }
        public required int Ordem { get; set; }
        public bool Obrigatoria { get; set; } = false;

        public int FormularioId { get; set; }
        public FormularioModel? Formulario { get; set; }

        public ICollection<AlternativaModel> Alternativas { get; set; } = new List<AlternativaModel>();
    }
}
