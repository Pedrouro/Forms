using System.ComponentModel.DataAnnotations;

namespace Forms.API.DTOs
{
    public class AlternativaDTO
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "O texto da alternativa é obrigatório.")]
        [StringLength(300, ErrorMessage = "O texto da alternativa deve ter no máximo 300 caracteres.")]
        public required string Texto { get; set; }

        public int? PerguntaId { get; set; }
    }
}
