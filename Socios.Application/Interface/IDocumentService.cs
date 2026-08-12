namespace Socios.Application.Interfaces;

public interface IDocumentService
{
    // Recibe el archivo físico, lo procesa y guarda los embeddings. Devuelve la cantidad de chunks guardados (0 si falló).
    Task<int> ProcessAndStoreDocumentAsync(string fileName, Stream fileStream, string contentType, int? clubId, CancellationToken cancellationToken = default);
}