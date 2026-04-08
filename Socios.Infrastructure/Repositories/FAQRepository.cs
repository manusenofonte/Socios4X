using Microsoft.EntityFrameworkCore;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using Socios.Infrastructure.Persistence;

namespace Socios.Infrastructure.Repositories;

public class FAQRepository : IFAQRepository
{
    private readonly ClubDbContext _context;

    public FAQRepository(ClubDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FrequentlyQuestion>> SearchRelevantFAQsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<FrequentlyQuestion>();

        var lowerQuery = query.ToLower();

        return await _context.FrequentlyQuestions
            .AsNoTracking()
            .Where(f => (f.Question != null && f.Question.ToLower().Contains(lowerQuery)) ||
                        (f.Keywords != null && f.Keywords.ToLower().Contains(lowerQuery)))
            .Take(5)
            .ToListAsync(cancellationToken);
    }
}