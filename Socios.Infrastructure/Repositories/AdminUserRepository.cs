using Microsoft.EntityFrameworkCore;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using Socios.Infrastructure.Persistence;

namespace Socios.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly ClubDbContext _context;

    public AdminUserRepository(ClubDbContext context)
    {
        _context = context;
    }

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<AdminUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AdminUser?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.AdminUsers.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.AdminUsers
            .AsNoTracking()
            .OrderByDescending(u => u.Role == AdminUser.RoleSuperAdmin)
            .ThenBy(u => u.Email)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
