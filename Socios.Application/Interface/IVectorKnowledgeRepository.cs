namespace Socios.Application.Interfaces;

public interface IVectorKnowledgeRepository
{
    // Busca chunks de documentos relevantes por similitud semántica (embeddings).
    // clubId null = sin filtrar por club.
    Task<IEnumerable<KnowledgeChunk>> SearchAsync(string query, int? clubId, CancellationToken cancellationToken = default);

    Task UpsertAsync(string source, int? clubId, IEnumerable<string> textChunks, CancellationToken cancellationToken = default);
}

public class KnowledgeChunk
{
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Source { get; set; } = string.Empty;
}
