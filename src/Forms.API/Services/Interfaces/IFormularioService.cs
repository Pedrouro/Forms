using Forms.API.DTOs;

namespace Forms.API.Services.Interfaces
{
    public interface IFormularioService
    {
        // Formulários
        Task<ResponseDTO<FormularioResponseDTO>> AddFormularioAsync(FormularioDTO dto);
        Task<FormularioResponseDTO> GetFormularioByIdAsync(int id);
        Task<IEnumerable<FormularioResponseDTO>> GetAllFormulariosAsync();
        Task<ResponseDTO> UpdateFormularioAsync(int id, FormularioDTO dto);
        Task<ResponseDTO> DeleteFormularioAsync(int id);
    }
}