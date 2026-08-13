using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using Socios.Infrastructure.Persistence;

namespace Socios.Infrastructure.Repositories;

public class FAQRepository : IFAQRepository
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "la", "el", "los", "las", "un", "una", "unos", "unas", "que", "como", "cual", "cuales",
        "es", "son", "y", "o", "en", "con", "para", "por", "del", "al", "se", "su", "sus", "a",
        "este", "esta", "estos", "estas", "puedo", "tengo", "hay", "the", "how", "to", "for", "of", "is"
    };

    private readonly ClubDbContext _context;

    public FAQRepository(ClubDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FrequentlyQuestion>> SearchRelevantFAQsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<FrequentlyQuestion>();

        var queryWords = ExtractKeywords(query);
        if (queryWords.Count == 0)
            return Enumerable.Empty<FrequentlyQuestion>();

        // Tabla chica (decenas de filas): traemos todo y puntuamos en memoria por palabras
        // compartidas, en vez de exigir coincidencia literal de la frase completa.
        var allFaqs = await _context.FrequentlyQuestions
            .AsNoTracking()
            .Where(f => f.Question != null)
            .ToListAsync(cancellationToken);

        return allFaqs
            .Select(f => new
            {
                Faq = f,
                Score = ExtractKeywords($"{f.Question} {f.Keywords}").Count(queryWords.Contains)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => x.Faq)
            .ToList();
    }

    public async Task<IEnumerable<FrequentlyQuestion>> GetMostFrequentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.FrequentlyQuestions
            .AsNoTracking()
            .Where(f => f.Question != null)
            .OrderByDescending(f => f.Frequently)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    private static HashSet<string> ExtractKeywords(string text)
    {
        var normalized = RemoveDiacritics(text.ToLowerInvariant());
        var words = normalized.Split(
            [' ', '?', '¿', '.', ',', '!', '¡', ':', ';', '(', ')', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries);

        return words.Where(w => w.Length > 2 && !StopWords.Contains(w)).ToHashSet();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}