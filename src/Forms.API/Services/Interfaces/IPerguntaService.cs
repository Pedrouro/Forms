using Forms.API.DTOs;

namespace Forms.API.Services.Interfaces
{
    public interface IPerguntaService
    {
        Task<ResponseDTO<PerguntaResponseDTO>> AddPerguntaAsync(int formularioId, PerguntaDTO dto);
        Task<PerguntaResponseDTO> GetPerguntaByIdAsync(int id);
        Task<IEnumerable<PerguntaResponseDTO>> GetPerguntasByFormularioIdAsync(int formularioId);
        Task<ResponseDTO<PerguntaResponseDTO>> UpdatePerguntaAsync(int id, PerguntaDTO dto);
        Task<ResponseDTO<IEnumerable<PerguntaResponseDTO>>> UpdatePerguntasPorFormularioAsync(int formularioId, IEnumerable<PerguntaDTO> dtos);
        Task<ResponseDTO> DeletePerguntaAsync(int id);
    }
}
