using Forms.API.DTOs;

namespace Forms.API.Services.Interfaces
{
    public interface IAlternativaService
    {
        Task<ResponseDTO<AlternativaResponseDTO>> AddAlternativaAsync(int perguntaId, AlternativaDTO dto);
        Task<AlternativaResponseDTO> GetAlternativaByIdAsync(int id);
        Task<IEnumerable<AlternativaResponseDTO>> GetAlternativasByPerguntaIdAsync(int perguntaId);
        Task<ResponseDTO<AlternativaResponseDTO>> UpdateAlternativaAsync(int id, AlternativaDTO dto);
        Task<ResponseDTO<IEnumerable<AlternativaResponseDTO>>> UpdateAlternativasPorPerguntaAsync(int perguntaId, IEnumerable<AlternativaDTO> dtos);
        Task<ResponseDTO> DeleteAlternativaAsync(int id);
    }
}
