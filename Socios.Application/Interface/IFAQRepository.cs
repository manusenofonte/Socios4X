using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IFAQRepository
{
    // Busca FAQs relevantes, con un score de relevancia comparable al de la búsqueda vectorial
    Task<IEnumerable<FAQMatch>> SearchRelevantFAQsAsync(string query, CancellationToken cancellationToken = default);

    // Las FAQs más consultadas, para mostrar como sugerencias
    Task<IEnumerable<FrequentlyQuestion>> GetMostFrequentAsync(int count, CancellationToken cancellationToken = default);
}

public class FAQMatch
{
    public FrequentlyQuestion Faq { get; set; } = null!;
    public double Score { get; set; }
}