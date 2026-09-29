using Forms.API.DTOs;
using Forms.API.Models;

namespace Forms.API.Mappers
{
    public static class AlternativaMapper
    {
        public static AlternativaModel ToModel(this AlternativaDTO dto, int perguntaId)
        {
            return new AlternativaModel
            {
                Texto = dto.Texto.Trim(),
                PerguntaId = perguntaId
            };
        }

        public static void ApplyUpdate(this AlternativaModel alternativa, AlternativaDTO dto)
        {
            alternativa.Texto = dto.Texto.Trim();
        }

        public static AlternativaResponseDTO ToResponseDTO(this AlternativaModel alternativa)
        {
            return new AlternativaResponseDTO
            {
                Id = alternativa.Id,
                Texto = alternativa.Texto,
                PerguntaId = alternativa.PerguntaId
            };
        }
    }
}
