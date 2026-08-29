using Microsoft.EntityFrameworkCore;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using Socios.Infrastructure.Persistence;

namespace Socios.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ClubDbContext _context;

    public DocumentRepository(ClubDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(int? clubId, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (clubId.HasValue)
            query = query.Where(d => d.ClubId == clubId.Value);

        return await query
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
