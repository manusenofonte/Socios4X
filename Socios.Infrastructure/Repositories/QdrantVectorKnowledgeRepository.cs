using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Socios.Application.Interfaces;

namespace Socios.Infrastructure.Repositories;

public class QdrantVectorKnowledgeRepository : IVectorKnowledgeRepository
{
    private const string CollectionName = "socios_knowledge";
    private const ulong VectorSize = 1536; // text-embedding-3-small

    // Similitud coseno mínima para considerar un chunk relevante. Valor de partida a
    // calibrar con documentos reales: por debajo de esto, se descarta antes de llegar
    // al prompt en vez de confiar en que el modelo lo ignore.
    private const float MinScoreThreshold = 0.35f;

    private readonly QdrantClient _client;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public QdrantVectorKnowledgeRepository(QdrantClient client, ITextEmbeddingGenerationService embeddingService)
    {
        _client = client;
        _embeddingService = embeddingService;
    }

    public async Task<IEnumerable<KnowledgeChunk>> SearchAsync(string query, int? clubId, CancellationToken cancellationToken = default)
    {
        if (!await _client.CollectionExistsAsync(CollectionName, cancellationToken))
            return Enumerable.Empty<KnowledgeChunk>();

        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

        Filter? filter = null;
        if (clubId.HasValue)
        {
            filter = new Filter();
            filter.Must.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "clubId",
                    Match = new Match { Integer = clubId.Value }
                }
            });
        }

        var results = await _client.SearchAsync(
            CollectionName,
            embedding.ToArray(),
            filter: filter,
            limit: 5,
            scoreThreshold: MinScoreThreshold,
            cancellationToken: cancellationToken);

        return results.Select(point => new KnowledgeChunk
        {
            Text = point.Payload.TryGetValue("text", out var text) ? text.StringValue : string.Empty,
            Score = point.Score,
            Source = point.Payload.TryGetValue("source", out var source) ? source.StringValue : "Documento"
        });
    }

    public async Task UpsertAsync(string source, int? clubId, IEnumerable<string> textChunks, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var points = new List<PointStruct>();

        foreach (var chunk in textChunks)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk, cancellationToken: cancellationToken);

            var point = new PointStruct
            {
                Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                Vectors = embedding.ToArray()
            };
            point.Payload["text"] = chunk;
            point.Payload["source"] = source;
            if (clubId.HasValue)
                point.Payload["clubId"] = clubId.Value;

            points.Add(point);
        }

        if (points.Count > 0)
            await _client.UpsertAsync(CollectionName, points, cancellationToken: cancellationToken);
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        if (await _client.CollectionExistsAsync(CollectionName, cancellationToken))
            return;

        await _client.CreateCollectionAsync(
            CollectionName,
            new VectorParams { Size = VectorSize, Distance = Distance.Cosine },
            cancellationToken: cancellationToken);
    }
}
