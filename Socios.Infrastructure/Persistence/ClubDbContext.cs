using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Socios.Domain.Entities;

namespace Socios.Infrastructure.Persistence;

public class ClubDbContext : DbContext
{
    public ClubDbContext(DbContextOptions<ClubDbContext> options) : base(options) { }

    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    public DbSet<FrequentlyQuestion> FrequentlyQuestions { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<UnansweredQuestion> UnansweredQuestions { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }

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

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UploadDate).HasConversion(UtcConverter);
            entity.Property(e => e.ProcessedDate).HasConversion(NullableUtcConverter);
        });

        modelBuilder.Entity<UnansweredQuestion>(entity =>
        {
            entity.ToTable("UnansweredQuestions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Question).HasMaxLength(UnansweredQuestion.MaxQuestionLength);
            entity.Property(e => e.CreatedAt).HasConversion(UtcConverter);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.PasswordResetTokenHash).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasConversion(UtcConverter);
            entity.Property(e => e.LastLoginAt).HasConversion(NullableUtcConverter);
            entity.Property(e => e.LockoutEndsAt).HasConversion(NullableUtcConverter);
            entity.Property(e => e.TemporaryPasswordExpiresAt).HasConversion(NullableUtcConverter);
            entity.Property(e => e.PasswordResetTokenExpiresAt).HasConversion(NullableUtcConverter);
            entity.Ignore(e => e.IsSuperAdmin);
        });
    }

    // Las fechas se guardan en UTC, pero datetime2 no guarda la zona: al leerlas las
    // marcamos como UTC para que el JSON salga con "Z" y el navegador las pase a hora local.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}