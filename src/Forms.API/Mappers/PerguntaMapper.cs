using Forms.API.DTOs;
using Forms.API.Models;

namespace Forms.API.Mappers
{
    public static class PerguntaMapper
    {
        public static PerguntaModel ToModel(this PerguntaDTO dto, int formularioId, int ordem)
        {
            var pergunta = new PerguntaModel
            {
                Enunciado = dto.Enunciado.Trim(),
                Tipo = dto.Tipo,
                Ordem = ordem,
                Obrigatoria = dto.Obrigatoria,
                FormularioId = formularioId
            };

            if (dto.Alternativas != null && dto.Alternativas.Any())
            {
                pergunta.Alternativas = dto.Alternativas.Select(a => new AlternativaModel
                {
                    Texto = a.Texto.Trim()
                }).ToList();
            }

            return pergunta;
        }

        public static void ApplyUpdate(this PerguntaModel pergunta, PerguntaDTO dto)
        {
            pergunta.Enunciado = dto.Enunciado.Trim();
            pergunta.Tipo = dto.Tipo;
            pergunta.Ordem = dto.Ordem;
            pergunta.Obrigatoria = dto.Obrigatoria;
        }

        public static PerguntaResponseDTO ToResponseDTO(this PerguntaModel pergunta)
        {
            return new PerguntaResponseDTO
            {
                Id = pergunta.Id,
                Enunciado = pergunta.Enunciado,
                Tipo = pergunta.Tipo,
                Ordem = pergunta.Ordem,
                Obrigatoria = pergunta.Obrigatoria,
                FormularioId = pergunta.FormularioId,
                Alternativas = pergunta.Alternativas?
                    .Select(a => a.ToResponseDTO())
                    .ToList() ?? new List<AlternativaResponseDTO>()
            };
        }
    }
}
