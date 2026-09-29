using Forms.API.Data;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Forms.API.Repositories.Implementations
{
    public class FormularioRepository : IFormularioRepository
    {
        private readonly FormsDbContext _dbContext;

        public FormularioRepository(FormsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<FormularioModel> AddAsync(FormularioModel formulario)
        {
            await _dbContext.Formulario.AddAsync(formulario);
            await _dbContext.SaveChangesAsync();
            return formulario;
        }

        public async Task<FormularioModel?> GetByIdAsync(int id)
        {
            return await _dbContext.Formulario
                .Include(f => f.Criador)
                .Include(f => f.Perguntas.OrderBy(p => p.Ordem))
                    .ThenInclude(p => p.Alternativas)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<FormularioModel>> GetAllAsync()
        {
            return await _dbContext.Formulario
                .Include(f => f.Perguntas.OrderBy(p => p.Ordem))
                    .ThenInclude(p => p.Alternativas)
                .ToListAsync();
        }

        public async Task UpdateAsync(FormularioModel formulario)
        {
            _dbContext.Formulario.Update(formulario);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(FormularioModel formulario)
        {
            _dbContext.Formulario.Remove(formulario);
            await _dbContext.SaveChangesAsync();
        }
    }
}