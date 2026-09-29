using Forms.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace Forms.API.DTOs
{
    public class FormularioDTO
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public required string Titulo { get; set; }

        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
        public string? Descricao { get; set; }

        public StatusFormulario Status { get; set; } = StatusFormulario.Rascunho;

        public List<PerguntaDTO>? Perguntas { get; set; }
    }
}
