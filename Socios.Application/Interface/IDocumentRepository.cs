using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    Task<IEnumerable<Document>> GetAllAsync(int? clubId, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Document document, CancellationToken cancellationToken = default);
}
