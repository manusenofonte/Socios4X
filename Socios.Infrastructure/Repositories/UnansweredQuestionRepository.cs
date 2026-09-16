using Microsoft.EntityFrameworkCore;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using Socios.Infrastructure.Persistence;

namespace Socios.Infrastructure.Repositories;

public class UnansweredQuestionRepository : IUnansweredQuestionRepository
{
    private readonly ClubDbContext _context;

    public UnansweredQuestionRepository(ClubDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UnansweredQuestion question, CancellationToken cancellationToken = default)
    {
        _context.UnansweredQuestions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<UnansweredQuestion>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.UnansweredQuestions.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        // Desempate por Id para que el orden sea estable entre páginas
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UnansweredQuestion>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _context.UnansweredQuestions
            .Where(q => q.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
