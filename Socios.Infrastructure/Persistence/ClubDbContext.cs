using Microsoft.EntityFrameworkCore;
using Socios.Domain.Entities;

namespace Socios.Infrastructure.Persistence;

public class ClubDbContext : DbContext
{
    public ClubDbContext(DbContextOptions<ClubDbContext> options) : base(options) { }

    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    public DbSet<FrequentlyQuestion> FrequentlyQuestions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(e => e.Id);
        });

        // AÑADIDO: Buenas prácticas, mapeo explícito de la tabla
        modelBuilder.Entity<FrequentlyQuestion>(entity =>
        {
            entity.ToTable("FrequentlyQuestions");
            entity.HasKey(e => e.Id);
            // Si las Keywords pueden ser nulas, lo aseguramos acá
            entity.Property(e => e.Keywords).IsRequired(false);
        });
    }
}