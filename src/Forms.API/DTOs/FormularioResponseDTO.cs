using Forms.API.Enums;

namespace Forms.API.DTOs
{
    public class FormularioResponseDTO
    {
        public int Id { get; set; }
        public required string Titulo { get; set; }
        public string? Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public StatusFormulario Status { get; set; }

        public List<PerguntaResponseDTO> Perguntas { get; set; } = new();
    }
}