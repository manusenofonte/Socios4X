using Socios.Application.Interfaces;

namespace Socios.Infrastructure.Repositories;

// Placeholder hasta que se implemente la búsqueda vectorial real (Qdrant).
// Devuelve siempre vacío para que el asistente funcione hoy solo con las FAQs.
public class NullVectorKnowledgeRepository : IVectorKnowledgeRepository
{
    public Task<IEnumerable<KnowledgeChunk>> SearchAsync(string query, int? clubId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<KnowledgeChunk>());
    }

    public Task UpsertAsync(string source, int? clubId, IEnumerable<string> textChunks, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
