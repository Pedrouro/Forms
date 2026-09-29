using Forms.API.Data;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Forms.API.Repositories.Implementations
{
    public class PerguntaRepository : IPerguntaRepository
    {
        private readonly FormsDbContext _dbContext;

        public PerguntaRepository(FormsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PerguntaModel> AddAsync(PerguntaModel pergunta)
        {
            await _dbContext.Pergunta.AddAsync(pergunta);
            await _dbContext.SaveChangesAsync();
            return pergunta;
        }

        public async Task DeleteAsync(PerguntaModel pergunta)
        {
            _dbContext.Pergunta.Remove(pergunta);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            PerguntaModel? pergunta = await _dbContext.Pergunta.FindAsync(id);
            if (pergunta != null)
            {
                _dbContext.Pergunta.Remove(pergunta);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PerguntaModel>> GetAllAsync()
        {
            return await _dbContext.Pergunta
                .Include(p => p.Alternativas)
                .OrderBy(p => p.Ordem)
                .ToListAsync();
        }

        public async Task<PerguntaModel?> GetByIdAsync(int id)
        {
            return await _dbContext.Pergunta
                .Include(p => p.Alternativas)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<PerguntaModel>> GetByFormularioIdAsync(int formularioId)
        {
            return await _dbContext.Pergunta
                .Include(p => p.Alternativas)
                .Where(p => p.FormularioId == formularioId)
                .OrderBy(p => p.Ordem)
                .ToListAsync();
        }

        public async Task<int> GetMaxOrdemByFormularioIdAsync(int formularioId)
        {
            return await _dbContext.Pergunta
                .Where(p => p.FormularioId == formularioId)
                .MaxAsync(p => (int?)p.Ordem) ?? 0;
        }

        public async Task UpdateAsync(PerguntaModel pergunta)
        {
            _dbContext.Pergunta.Update(pergunta);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<PerguntaModel> perguntas)
        {
            _dbContext.Pergunta.UpdateRange(perguntas);
            await _dbContext.SaveChangesAsync();
        }
    }
}
