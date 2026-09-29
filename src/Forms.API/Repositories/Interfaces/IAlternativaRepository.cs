using Forms.API.Models;

namespace Forms.API.Repositories.Interfaces
{
    public interface IAlternativaRepository
    {
        Task<AlternativaModel?> GetByIdAsync(int id);
        Task<IEnumerable<AlternativaModel>> GetAllAsync();
        Task<IEnumerable<AlternativaModel>> GetByPerguntaIdAsync(int perguntaId);
        Task<AlternativaModel> AddAsync(AlternativaModel alternativa);
        Task UpdateAsync(AlternativaModel alternativa);
        Task UpdateRangeAsync(IEnumerable<AlternativaModel> alternativas);
        Task DeleteAsync(AlternativaModel alternativa);
        Task DeleteAsync(int id);
    }
}
