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

        // 2. Búsqueda en repositorio vectorial (clubId null = sin filtrar por club)
        var documentChunks = await _vectorRepository.SearchAsync(userQuery, clubId, cancellationToken);

        // 3. Normalización de FAQs como chunks (score real de coincidencia, no fijo)
        var faqChunks = faqs.Select(m => new KnowledgeChunk
        {
            Text = $"Pregunta: {m.Faq.Question}\nRespuesta: {m.Faq.Answer}",
            Score = m.Score,
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
- Podés sintetizar, reformular o combinar datos del contexto para responder, aunque la pregunta no use las mismas palabras exactas que el contexto. Por ejemplo, si el contexto describe cómo un socio accede a una función, eso responde una pregunta sobre cómo hacer esa función, aunque el contexto no use la palabra 'cómo'.
- Si después de leer todo el contexto la información pedida simplemente no está ahí (ni siquiera de forma indirecta), responde exactamente:
  'Lo siento, no tengo información sobre ese tema. Te recomiendo contactarte con la administración del club o 4x.'
- No inventar datos que no estén en el contexto (nombres, números, plazos, precios, pasos que no se mencionan).
- No utilizar conocimiento externo al contexto.
- Priorizar precisión sobre cantidad.

Formato:
- Respuesta breve y profesional.
- Usa Markdown: negrita para lo importante, listas para enumeraciones.
- Si la respuesta es una secuencia de pasos, usa una lista numerada con un paso por línea (nunca todos los pasos seguidos en el mismo párrafo).

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