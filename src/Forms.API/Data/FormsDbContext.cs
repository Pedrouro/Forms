using Forms.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Forms.API.Data
{
    public class FormsDbContext : DbContext
    {
        public FormsDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<FormularioModel> Formulario { get; set; }
        public DbSet<PerguntaModel> Pergunta { get; set; }
        public DbSet<RespostaModel> Resposta { get; set; }
        public DbSet<UsuarioModel> Usuario { get; set; }
        public DbSet<EnvioModel> Envio { get; set; }
        public DbSet<AlternativaModel> Alternativa { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FormsDbContext).Assembly);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsuarioModel>()
                .Property(u => u.Perfil)
                .HasConversion<string>();

            modelBuilder.Entity<PerguntaModel>()
                .Property(p => p.Tipo)
                .HasConversion<string>();

            modelBuilder.Entity<FormularioModel>()
                .HasMany(f => f.Perguntas)
                .WithOne(p => p.Formulario)
                .HasForeignKey(p => p.FormularioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PerguntaModel>()
                .HasMany(p => p.Alternativas)
                .WithOne(a => a.Pergunta)
                .HasForeignKey(a => a.PerguntaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
