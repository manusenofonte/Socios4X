using Microsoft.SemanticKernel;
using Socios.Application.Interfaces;
using System.Text;

namespace Socios.Application.Services;

public class VirtualAssistantService : IVirtualAssistantService
{
    private readonly Kernel _kernel;
    private readonly IFAQRepository _faqRepository;

    // El repositorio vectorial lo inyectaremos en el próximo módulo
    // private readonly IVectorKnowledgeRepository _vectorRepository;

    public VirtualAssistantService(Kernel kernel, IFAQRepository faqRepository)
    {
        _kernel = kernel;
        _faqRepository = faqRepository;
    }

    public async Task<string> AskQuestionAsync(string userQuery, string? tenantId, CancellationToken cancellationToken = default)
    {
        // 1. Buscar coincidencias en FAQs (Alta prioridad)
        var faqs = await _faqRepository.SearchRelevantFAQsAsync(userQuery, cancellationToken);

        // 2. Buscar en base vectorial (Documentos PDFs/Words)
        // var documentChunks = await _vectorRepository.SearchAsync(userQuery, tenantId, cancellationToken);

        // 3. Construir el contexto combinado
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("--- INICIO DEL CONTEXTO ---");

        contextBuilder.AppendLine("Información Estructurada (FAQs):");
        foreach (var faq in faqs)
        {
            // Solo incluimos la respuesta si la pregunta es relevante
            contextBuilder.AppendLine($"- Pregunta: {faq.Question} | Respuesta: {faq.Answer}");
        }

        contextBuilder.AppendLine("\nInformación de Documentos (Manuales/PDFs):");
        // foreach (var chunk in documentChunks) { contextBuilder.AppendLine(chunk.Text); }
        contextBuilder.AppendLine("No hay documentos adicionales en este momento.");

        contextBuilder.AppendLine("--- FIN DEL CONTEXTO ---");

        // 4. Prompt Engineering estricto (System Prompt)
        var prompt = @"
        Eres un asistente virtual corporativo para un club. Tu objetivo es responder a la pregunta del usuario utilizando ÚNICAMENTE la información provista en la sección de Contexto.
        
        Reglas estrictas:
        - Si la respuesta a la pregunta no se encuentra explícitamente en el Contexto, debes responder exactamente: 'Lo siento, no tengo información sobre ese tema en mis registros actuales.'
        - NO inventes información, NO asumas datos, y NO utilices conocimiento externo a este prompt.
        - Sé claro, profesional y conciso.

        Contexto provisto:
        {{$context}}

        Pregunta del usuario: {{$query}}
        ";

        /* 5. Configurar los argumentos y ejecutar Semantic Kernel */
        var arguments = new KernelArguments
        {
            { "context", contextBuilder.ToString() },
            { "query", userQuery }
        };

        var result = await _kernel.InvokePromptAsync(prompt, arguments, cancellationToken: cancellationToken);

        return result.ToString();
    }
}