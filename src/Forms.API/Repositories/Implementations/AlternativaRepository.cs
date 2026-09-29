using Forms.API.Data;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Forms.API.Repositories.Implementations
{
    public class AlternativaRepository : IAlternativaRepository
    {
        private readonly FormsDbContext _dbContext;

        public AlternativaRepository(FormsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AlternativaModel> AddAsync(AlternativaModel alternativa)
        {
            await _dbContext.Alternativa.AddAsync(alternativa);
            await _dbContext.SaveChangesAsync();
            return alternativa;
        }

        public async Task DeleteAsync(AlternativaModel alternativa)
        {
            _dbContext.Alternativa.Remove(alternativa);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            AlternativaModel? alternativa = await _dbContext.Alternativa.FindAsync(id);
            if (alternativa != null)
            {
                _dbContext.Alternativa.Remove(alternativa);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<AlternativaModel>> GetAllAsync()
        {
            return await _dbContext.Alternativa.ToListAsync();
        }

        public async Task<AlternativaModel?> GetByIdAsync(int id)
        {
            return await _dbContext.Alternativa
                .Include(a => a.Pergunta)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AlternativaModel>> GetByPerguntaIdAsync(int perguntaId)
        {
            return await _dbContext.Alternativa
                .Where(a => a.PerguntaId == perguntaId)
                .ToListAsync();
        }

        public async Task UpdateAsync(AlternativaModel alternativa)
        {
            _dbContext.Alternativa.Update(alternativa);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<AlternativaModel> alternativas)
        {
            _dbContext.Alternativa.UpdateRange(alternativas);
            await _dbContext.SaveChangesAsync();
        }
    }
}
