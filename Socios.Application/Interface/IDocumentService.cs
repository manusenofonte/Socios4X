using Socios.Application.DTOs;
using Socios.Domain.Entities;

namespace Socios.Application.Interfaces;

public interface IDocumentService
{
    // Recibe el archivo físico, lo procesa y guarda los embeddings. Devuelve la cantidad de chunks guardados (0 si falló).
    Task<int> ProcessAndStoreDocumentAsync(string fileName, Stream fileStream, string contentType, int? clubId, CancellationToken cancellationToken = default);

    // Página de documentos, del más reciente al más viejo
    Task<PagedResult<Document>> GetDocumentsAsync(int? clubId, int page, int pageSize, CancellationToken cancellationToken = default);

    // true si existía y se borró (registro + chunks vectoriales); false si no existía.
    Task<bool> DeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default);

    // true si existía y se actualizó; false si no existía.
    Task<bool> UpdateDocumentAsync(int documentId, string fileName, int? clubId, CancellationToken cancellationToken = default);
}
