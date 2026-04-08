using Microsoft.SemanticKernel;
using Socios.Application.Interfaces;
using System.Text;

namespace Socios.Application.Services;

public class VirtualAssistantService : IVirtualAssistantService
{
    private readonly Kernel _kernel;
    private readonly IFAQRepository _faqRepository;
    private readonly IVectorKnowledgeRepository _vectorRepository;

    public VirtualAssistantService(
        Kernel kernel,
        IFAQRepository faqRepository,
        IVectorKnowledgeRepository vectorRepository)
    {
        _kernel = kernel;
        _faqRepository = faqRepository;
        _vectorRepository = vectorRepository;
    }

    public async Task<string> AskQuestionAsync(
        string userQuery,
        int? clubId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("La consulta no puede estar vacía.");

        // 1. Búsqueda de FAQs relevantes
        var faqs = await _faqRepository
            .SearchRelevantFAQsAsync(userQuery, cancellationToken);

        // 2. Búsqueda en repositorio vectorial
        var documentChunks = clubId.HasValue
            ? await _vectorRepository.SearchAsync(userQuery, clubId.Value, cancellationToken)
            : Enumerable.Empty<KnowledgeChunk>();

        // 3. Normalización de FAQs como chunks
        var faqChunks = faqs.Select(f => new KnowledgeChunk
        {
            Text = $"Pregunta: {f.Question}\nRespuesta: {f.Answer}",
            Score = 1.0,
            Source = "FAQ"
        });

        // 4. Unificación y ordenamiento por relevancia
        var allChunks = faqChunks
            .Concat(documentChunks)
            .OrderByDescending(c => c.Score)
            .Take(6)
            .ToList();

        // 5. Construcción del contexto
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("--- CONTEXTO RELEVANTE ---");

        if (allChunks.Any())
        {
            foreach (var chunk in allChunks)
            {
                contextBuilder.AppendLine($"[{chunk.Source}] {chunk.Text}");
            }
        }
        else
        {
            contextBuilder.AppendLine("No hay información relevante.");
        }

        contextBuilder.AppendLine("--- FIN CONTEXTO ---");

        // 6. Definición del prompt
        var prompt = @"
Eres un asistente virtual de un club.

Tu tarea es responder exclusivamente utilizando el contexto proporcionado.

Reglas:
- Si la respuesta no se encuentra explícitamente en el contexto, responde exactamente:
  'Lo siento, no tengo información sobre ese tema en mis registros actuales.'
- No inventar información.
- No utilizar conocimiento externo al contexto.
- Priorizar precisión sobre cantidad.

Formato:
- Respuesta breve y profesional.

Contexto:
{{$context}}

Pregunta:
{{$query}}
";

        var arguments = new KernelArguments
        {
            ["context"] = contextBuilder.ToString(),
            ["query"] = userQuery
        };

        var result = await _kernel.InvokePromptAsync(
            prompt,
            arguments,
            cancellationToken: cancellationToken);

        return result.ToString();
    }
}