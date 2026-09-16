using Socios.Application.DTOs;
using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    Task<PagedResult<Document>> GetPagedAsync(int? clubId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Document document, CancellationToken cancellationToken = default);
}
