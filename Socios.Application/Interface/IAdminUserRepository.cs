using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AdminUser user, CancellationToken cancellationToken = default);

    // Guarda los cambios hechos sobre usuarios obtenidos de este repositorio
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
