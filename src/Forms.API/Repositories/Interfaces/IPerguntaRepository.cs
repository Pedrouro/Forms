using Forms.API.Models;

namespace Forms.API.Repositories.Interfaces
{
    public interface IPerguntaRepository
    {
        Task<PerguntaModel?> GetByIdAsync(int id);
        Task<IEnumerable<PerguntaModel>> GetAllAsync();
        Task<IEnumerable<PerguntaModel>> GetByFormularioIdAsync(int formularioId);
        Task<int> GetMaxOrdemByFormularioIdAsync(int formularioId);
        Task<PerguntaModel> AddAsync(PerguntaModel pergunta);
        Task UpdateAsync(PerguntaModel pergunta);
        Task UpdateRangeAsync(IEnumerable<PerguntaModel> perguntas);
        Task DeleteAsync(PerguntaModel pergunta);
        Task DeleteAsync(int id);
    }
}
