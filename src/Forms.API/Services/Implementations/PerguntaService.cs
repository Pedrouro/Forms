using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Mappers;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Interfaces;

namespace Forms.API.Services.Implementations
{
    public class PerguntaService : IPerguntaService
    {
        private readonly IPerguntaRepository _perguntaRepository;
        private readonly IFormularioRepository _formularioRepository;
        private readonly IAlternativaRepository _alternativaRepository;
        private readonly ICurrentUserService _currentUserService;

        public PerguntaService(
            IPerguntaRepository perguntaRepository,
            IFormularioRepository formularioRepository,
            IAlternativaRepository alternativaRepository,
            ICurrentUserService currentUserService)
        {
            _perguntaRepository = perguntaRepository;
            _formularioRepository = formularioRepository;
            _alternativaRepository = alternativaRepository;
            _currentUserService = currentUserService;
        }

        public async Task<ResponseDTO<PerguntaResponseDTO>> AddPerguntaAsync(int formularioId, PerguntaDTO dto)
        {
            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(formularioId);

            if (!dto.Tipo.PermiteAlternativas() && dto.Alternativas != null && dto.Alternativas.Any())
                throw new ArgumentException($"Perguntas do tipo '{dto.Tipo}' não aceitam alternativas.");

            if (dto.Alternativas != null && dto.Alternativas.Any(a => string.IsNullOrWhiteSpace(a.Texto)))
                throw new ArgumentException("O texto da alternativa não pode ser vazio.");

            int ordem = dto.Ordem > 0
                ? dto.Ordem
                : await _perguntaRepository.GetMaxOrdemByFormularioIdAsync(formularioId) + 1;

            PerguntaModel pergunta = dto.ToModel(formularioId, ordem);
            await _perguntaRepository.AddAsync(pergunta);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<PerguntaResponseDTO>
            {
                Status = true,
                Message = "Pergunta adicionada com sucesso.",
                Data = pergunta.ToResponseDTO()
            };
        }

        public async Task<PerguntaResponseDTO> GetPerguntaByIdAsync(int id)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Pergunta com id {id} não encontrada.");

            await ObterFormularioParaLeituraAsync(pergunta.FormularioId);

            return pergunta.ToResponseDTO();
        }

        public async Task<IEnumerable<PerguntaResponseDTO>> GetPerguntasByFormularioIdAsync(int formularioId)
        {
            await ObterFormularioParaLeituraAsync(formularioId);

            IEnumerable<PerguntaModel> perguntas = await _perguntaRepository.GetByFormularioIdAsync(formularioId);
            return perguntas.Select(p => p.ToResponseDTO());
        }

        public async Task<ResponseDTO<PerguntaResponseDTO>> UpdatePerguntaAsync(int id, PerguntaDTO dto)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Pergunta com id {id} não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            if (!dto.Tipo.PermiteAlternativas() && dto.Alternativas != null && dto.Alternativas.Any())
                throw new ArgumentException($"Perguntas do tipo '{dto.Tipo}' não aceitam alternativas.");

            // Se o tipo for alterado para um que não permite alternativas, remove as alternativas existentes
            if (!dto.Tipo.PermiteAlternativas() && pergunta.Alternativas.Any())
            {
                foreach (var alternativa in pergunta.Alternativas.ToList())
                {
                    await _alternativaRepository.DeleteAsync(alternativa);
                }
                pergunta.Alternativas.Clear();
            }

            pergunta.ApplyUpdate(dto);
            await _perguntaRepository.UpdateAsync(pergunta);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<PerguntaResponseDTO>
            {
                Status = true,
                Message = "Pergunta atualizada com sucesso.",
                Data = pergunta.ToResponseDTO()
            };
        }

        public async Task<ResponseDTO<IEnumerable<PerguntaResponseDTO>>> UpdatePerguntasPorFormularioAsync(int formularioId, IEnumerable<PerguntaDTO> dtos)
        {
            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(formularioId);

            if (dtos == null || !dtos.Any())
                throw new ArgumentException("A lista de perguntas não pode ser vazia.");

            if (dtos.Any(d => !d.Id.HasValue || d.Id.Value <= 0))
                throw new ArgumentException("Todas as perguntas devem conter um Id válido para atualização.");

            if (dtos.Select(d => d.Id!.Value).Distinct().Count() != dtos.Count())
                throw new ArgumentException("A lista contém IDs de perguntas duplicados.");

            if (dtos.Any(d => d.FormularioId.HasValue && d.FormularioId.Value != formularioId))
                throw new ArgumentException($"Todas as perguntas precisam pertencer ao mesmo formulário (id {formularioId}).");

            IEnumerable<PerguntaModel> perguntasDb = await _perguntaRepository.GetByFormularioIdAsync(formularioId);
            var perguntasDbDict = perguntasDb.ToDictionary(p => p.Id);

            foreach (var dto in dtos)
            {
                if (!perguntasDbDict.TryGetValue(dto.Id!.Value, out _))
                    throw new ArgumentException($"A pergunta com id {dto.Id!.Value} não pertence ao formulário informado.");

                if (!dto.Tipo.PermiteAlternativas() && dto.Alternativas != null && dto.Alternativas.Any())
                    throw new ArgumentException($"Perguntas do tipo '{dto.Tipo}' não aceitam alternativas.");
            }

            var perguntasAtualizadas = new List<PerguntaModel>();

            foreach (var dto in dtos)
            {
                var pergunta = perguntasDbDict[dto.Id!.Value];

                // Se o tipo for alterado para um que não permite alternativas, remove as alternativas existentes
                if (!dto.Tipo.PermiteAlternativas() && pergunta.Alternativas.Any())
                {
                    foreach (var alternativa in pergunta.Alternativas.ToList())
                    {
                        await _alternativaRepository.DeleteAsync(alternativa);
                    }
                    pergunta.Alternativas.Clear();
                }

                pergunta.ApplyUpdate(dto);
                perguntasAtualizadas.Add(pergunta);
            }

            await _perguntaRepository.UpdateRangeAsync(perguntasAtualizadas);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<IEnumerable<PerguntaResponseDTO>>
            {
                Status = true,
                Message = "Perguntas atualizadas com sucesso.",
                Data = perguntasAtualizadas.OrderBy(p => p.Ordem).Select(p => p.ToResponseDTO())
            };
        }

        public async Task<ResponseDTO> DeletePerguntaAsync(int id)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Pergunta com id {id} não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            await _perguntaRepository.DeleteAsync(pergunta);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO
            {
                Status = true,
                Message = "Pergunta deletada com sucesso."
            };
        }

        #region Métodos Auxiliares

        private async Task<FormularioModel> ObterFormularioParaEdicaoAsync(int formularioId)
        {
            FormularioModel formulario = await _formularioRepository.GetByIdAsync(formularioId)
                ?? throw new NotFoundException($"Formulário com id {formularioId} não encontrado.");

            if (!UsuarioPodeAcessar(formulario))
                throw new ForbiddenException("Você não tem permissão para alterar este formulário.");

            if (formulario.Status == StatusFormulario.Encerrado)
                throw new ArgumentException("Não é possível modificar um formulário encerrado.");

            return formulario;
        }

        private async Task<FormularioModel> ObterFormularioParaLeituraAsync(int formularioId)
        {
            FormularioModel formulario = await _formularioRepository.GetByIdAsync(formularioId)
                ?? throw new NotFoundException($"Formulário com id {formularioId} não encontrado.");

            bool podeVer = UsuarioPodeAcessar(formulario) || formulario.Status == StatusFormulario.Publicado;
            if (!podeVer)
                throw new NotFoundException($"Formulário com id {formularioId} não encontrado.");

            return formulario;
        }

        private bool UsuarioPodeAcessar(FormularioModel formulario)
        {
            int currentUsuarioId = _currentUserService.GetCurrentUserId();
            PerfilUsuario currentPerfil = _currentUserService.GetCurrentUserPerfil();

            bool isDono = formulario.CriadorId == currentUsuarioId;
            bool isAdmin = currentPerfil == PerfilUsuario.Administrador;

            return isDono || isAdmin;
        }

        #endregion
    }
}
