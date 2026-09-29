using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Mappers;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Interfaces;

namespace Forms.API.Services.Implementations
{
    public class AlternativaService : IAlternativaService
    {
        private readonly IAlternativaRepository _alternativaRepository;
        private readonly IPerguntaRepository _perguntaRepository;
        private readonly IFormularioRepository _formularioRepository;
        private readonly ICurrentUserService _currentUserService;

        public AlternativaService(
            IAlternativaRepository alternativaRepository,
            IPerguntaRepository perguntaRepository,
            IFormularioRepository formularioRepository,
            ICurrentUserService currentUserService)
        {
            _alternativaRepository = alternativaRepository;
            _perguntaRepository = perguntaRepository;
            _formularioRepository = formularioRepository;
            _currentUserService = currentUserService;
        }

        public async Task<ResponseDTO<AlternativaResponseDTO>> AddAlternativaAsync(int perguntaId, AlternativaDTO dto)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(perguntaId)
                ?? throw new NotFoundException($"Pergunta com id {perguntaId} não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            if (!pergunta.Tipo.PermiteAlternativas())
                throw new ArgumentException($"Não é possível adicionar alternativas a uma pergunta do tipo '{pergunta.Tipo}'. Apenas perguntas de múltipla escolha ou caixa de seleção aceitam alternativas.");

            if (string.IsNullOrWhiteSpace(dto.Texto))
                throw new ArgumentException("O texto da alternativa é obrigatório.");

            if (pergunta.Alternativas.Any(a => a.Texto.Trim().Equals(dto.Texto.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Já existe uma alternativa com esse texto nesta pergunta.");

            AlternativaModel alternativa = dto.ToModel(perguntaId);
            await _alternativaRepository.AddAsync(alternativa);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<AlternativaResponseDTO>
            {
                Status = true,
                Message = "Alternativa adicionada com sucesso.",
                Data = alternativa.ToResponseDTO()
            };
        }

        public async Task<IEnumerable<AlternativaResponseDTO>> GetAlternativasByPerguntaIdAsync(int perguntaId)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(perguntaId)
                ?? throw new NotFoundException($"Pergunta com id {perguntaId} não encontrada.");

            await ObterFormularioParaLeituraAsync(pergunta.FormularioId);

            return pergunta.Alternativas.Select(a => a.ToResponseDTO());
        }

        public async Task<AlternativaResponseDTO> GetAlternativaByIdAsync(int id)
        {
            AlternativaModel alternativa = await _alternativaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Alternativa com id {id} não encontrada.");

            PerguntaModel pergunta = alternativa.Pergunta
                ?? await _perguntaRepository.GetByIdAsync(alternativa.PerguntaId)
                ?? throw new NotFoundException("Pergunta associada à alternativa não encontrada.");

            await ObterFormularioParaLeituraAsync(pergunta.FormularioId);

            return alternativa.ToResponseDTO();
        }

        public async Task<ResponseDTO<AlternativaResponseDTO>> UpdateAlternativaAsync(int id, AlternativaDTO dto)
        {
            AlternativaModel alternativa = await _alternativaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Alternativa com id {id} não encontrada.");

            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(alternativa.PerguntaId)
                ?? throw new NotFoundException("Pergunta associada à alternativa não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            if (string.IsNullOrWhiteSpace(dto.Texto))
                throw new ArgumentException("O texto da alternativa é obrigatório.");

            if (pergunta.Alternativas.Any(a => a.Id != id && a.Texto.Trim().Equals(dto.Texto.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Já existe uma alternativa com esse texto nesta pergunta.");

            alternativa.ApplyUpdate(dto);
            await _alternativaRepository.UpdateAsync(alternativa);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<AlternativaResponseDTO>
            {
                Status = true,
                Message = "Alternativa atualizada com sucesso.",
                Data = alternativa.ToResponseDTO()
            };
        }

        public async Task<ResponseDTO<IEnumerable<AlternativaResponseDTO>>> UpdateAlternativasPorPerguntaAsync(int perguntaId, IEnumerable<AlternativaDTO> dtos)
        {
            PerguntaModel pergunta = await _perguntaRepository.GetByIdAsync(perguntaId)
                ?? throw new NotFoundException($"Pergunta com id {perguntaId} não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            if (!pergunta.Tipo.PermiteAlternativas())
                throw new ArgumentException($"Não é possível alterar alternativas de uma pergunta do tipo '{pergunta.Tipo}'.");

            if (dtos == null || !dtos.Any())
                throw new ArgumentException("A lista de alternativas não pode ser vazia.");

            if (dtos.Any(d => !d.Id.HasValue || d.Id.Value <= 0))
                throw new ArgumentException("Todas as alternativas devem conter um Id válido para atualização.");

            if (dtos.Select(d => d.Id!.Value).Distinct().Count() != dtos.Count())
                throw new ArgumentException("A lista contém IDs de alternativas duplicados.");

            if (dtos.Any(d => d.PerguntaId.HasValue && d.PerguntaId.Value != perguntaId))
                throw new ArgumentException($"Todas as alternativas precisam pertencer à mesma pergunta (id {perguntaId}).");

            if (dtos.Any(d => string.IsNullOrWhiteSpace(d.Texto)))
                throw new ArgumentException("O texto da alternativa é obrigatório.");

            IEnumerable<AlternativaModel> alternativasDb = await _alternativaRepository.GetByPerguntaIdAsync(perguntaId);
            var alternativasDbDict = alternativasDb.ToDictionary(a => a.Id);

            foreach (var dto in dtos)
            {
                if (!alternativasDbDict.TryGetValue(dto.Id!.Value, out _))
                    throw new ArgumentException($"A alternativa com id {dto.Id!.Value} não pertence à pergunta informada.");
            }

            var dtosDict = dtos.ToDictionary(d => d.Id!.Value);
            var textosFinais = alternativasDb.Select(a =>
                dtosDict.TryGetValue(a.Id, out var dto)
                    ? dto.Texto.Trim()
                    : a.Texto.Trim()
            ).ToList();

            if (textosFinais.GroupBy(t => t.ToLowerInvariant()).Any(g => g.Count() > 1))
                throw new ArgumentException("Já existe uma alternativa com esse texto nesta pergunta.");

            var alternativasAtualizadas = new List<AlternativaModel>();

            foreach (var dto in dtos)
            {
                var alternativa = alternativasDbDict[dto.Id!.Value];
                alternativa.ApplyUpdate(dto);
                alternativasAtualizadas.Add(alternativa);
            }

            await _alternativaRepository.UpdateRangeAsync(alternativasAtualizadas);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO<IEnumerable<AlternativaResponseDTO>>
            {
                Status = true,
                Message = "Alternativas atualizadas com sucesso.",
                Data = alternativasAtualizadas.Select(a => a.ToResponseDTO())
            };
        }

        public async Task<ResponseDTO> DeleteAlternativaAsync(int id)
        {
            AlternativaModel alternativa = await _alternativaRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Alternativa com id {id} não encontrada.");

            PerguntaModel pergunta = alternativa.Pergunta
                ?? await _perguntaRepository.GetByIdAsync(alternativa.PerguntaId)
                ?? throw new NotFoundException("Pergunta associada à alternativa não encontrada.");

            FormularioModel formulario = await ObterFormularioParaEdicaoAsync(pergunta.FormularioId);

            await _alternativaRepository.DeleteAsync(alternativa);

            formulario.DataAtualizacao = DateTime.UtcNow;
            await _formularioRepository.UpdateAsync(formulario);

            return new ResponseDTO
            {
                Status = true,
                Message = "Alternativa deletada com sucesso."
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
