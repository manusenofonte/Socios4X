using Socios.Application.DTOs;
using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IUnansweredQuestionRepository
{
    Task AddAsync(UnansweredQuestion question, CancellationToken cancellationToken = default);

    // Página de preguntas, de la más reciente a la más vieja
    Task<PagedResult<UnansweredQuestion>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    // true si existía y se borró; false si no existía.
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
