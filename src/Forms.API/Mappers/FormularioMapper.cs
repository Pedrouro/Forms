using Forms.API.DTOs;
using Forms.API.Models;

namespace Forms.API.Mappers
{
    public static class FormularioMapper
    {
        public static FormularioModel ToModel(this FormularioDTO dto, UsuarioModel criador)
        {
            var formulario = new FormularioModel
            {
                Titulo = dto.Titulo.Trim(),
                Descricao = dto.Descricao?.Trim(),
                Status = dto.Status,
                DataCriacao = DateTime.UtcNow,
                CriadorId = criador.Id,
                Criador = criador
            };

            if (dto.Perguntas != null && dto.Perguntas.Any())
            {
                int ordem = 1;
                formulario.Perguntas = dto.Perguntas.Select(p =>
                {
                    int itemOrdem = p.Ordem > 0 ? p.Ordem : ordem++;
                    return p.ToModel(0, itemOrdem);
                }).ToList();
            }

            return formulario;
        }

        public static void ApplyUpdate(this FormularioModel formulario, FormularioDTO dto)
        {
            formulario.Titulo = dto.Titulo.Trim();
            formulario.Descricao = dto.Descricao?.Trim();
            formulario.Status = dto.Status;
            formulario.DataAtualizacao = DateTime.UtcNow;
        }

        public static FormularioResponseDTO ToResponseDTO(this FormularioModel formulario)
        {
            return new FormularioResponseDTO
            {
                Id = formulario.Id,
                Titulo = formulario.Titulo,
                Descricao = formulario.Descricao,
                DataCriacao = formulario.DataCriacao,
                DataAtualizacao = formulario.DataAtualizacao,
                Status = formulario.Status,
                Perguntas = formulario.Perguntas?
                    .OrderBy(p => p.Ordem)
                    .Select(p => p.ToResponseDTO())
                    .ToList() ?? new List<PerguntaResponseDTO>()
            };
        }
    }
}