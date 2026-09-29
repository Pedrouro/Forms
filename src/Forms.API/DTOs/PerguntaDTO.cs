using Forms.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Forms.API.DTOs
{
    public class PerguntaDTO
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "O enunciado da pergunta é obrigatório.")]
        [StringLength(500, ErrorMessage = "O enunciado deve ter no máximo 500 caracteres.")]
        public required string Enunciado { get; set; }

        [Required(ErrorMessage = "O tipo da pergunta é obrigatório.")]
        public TipoPergunta Tipo { get; set; }

        public int Ordem { get; set; }

        public bool Obrigatoria { get; set; } = false;

        public int? FormularioId { get; set; }

        public List<AlternativaDTO>? Alternativas { get; set; }
    }
}
