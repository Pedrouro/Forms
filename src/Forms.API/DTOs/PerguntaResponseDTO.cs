using Forms.API.Enums;

namespace Forms.API.DTOs
{
    public class PerguntaResponseDTO
    {
        public int Id { get; set; }
        public required string Enunciado { get; set; }
        public TipoPergunta Tipo { get; set; }
        public int Ordem { get; set; }
        public bool Obrigatoria { get; set; }
        public int FormularioId { get; set; }
        public List<AlternativaResponseDTO> Alternativas { get; set; } = new();
    }
}
