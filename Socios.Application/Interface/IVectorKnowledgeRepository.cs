namespace Socios.Application.Interfaces;

public interface IVectorKnowledgeRepository
{
    // Busca chunks de documentos relevantes por similitud semántica (embeddings).
    // clubId null = sin filtrar por club.
    Task<IEnumerable<KnowledgeChunk>> SearchAsync(string query, int? clubId, CancellationToken cancellationToken = default);

    Task UpsertAsync(int documentId, string source, int? clubId, IEnumerable<string> textChunks, CancellationToken cancellationToken = default);

    // Borra todos los chunks vectoriales que pertenecen a un documento (al borrar el documento).
    Task DeleteByDocumentIdAsync(int documentId, CancellationToken cancellationToken = default);

    // Actualiza el "source" mostrado en los chunks de un documento (al renombrarlo).
    Task UpdateSourceAsync(int documentId, string newSource, CancellationToken cancellationToken = default);
}

public class KnowledgeChunk
{
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Source { get; set; } = string.Empty;
}
