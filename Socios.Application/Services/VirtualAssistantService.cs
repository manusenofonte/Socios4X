using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using System.Text;

namespace Socios.Application.Services;

public class VirtualAssistantService : IVirtualAssistantService
{
    public const string NoInformationMessage =
        "Lo siento, no tengo información sobre ese tema. Te recomiendo contactarte con la administración del club o 4x.";

    // Marcas que el modelo devuelve cuando no puede responder (ver prompt)
    internal const string NoInformationMarker = "[SIN_INFO]";
    internal const string OffTopicMarker = "[FUERA_DE_TEMA]";

    private readonly Kernel _kernel;
    private readonly IFAQRepository _faqRepository;
    private readonly IVectorKnowledgeRepository _vectorRepository;
    private readonly IUnansweredQuestionRepository _unansweredQuestionRepository;
    private readonly ILogger<VirtualAssistantService> _logger;

    public VirtualAssistantService(
        Kernel kernel,
        IFAQRepository faqRepository,
        IVectorKnowledgeRepository vectorRepository,
        IUnansweredQuestionRepository unansweredQuestionRepository,
        ILogger<VirtualAssistantService> logger)
    {
        _kernel = kernel;
        _faqRepository = faqRepository;
        _vectorRepository = vectorRepository;
        _unansweredQuestionRepository = unansweredQuestionRepository;
        _logger = logger;
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
Eres el asistente virtual de 4x para los clubes que usan su Sistema de Gestión de Socios.

Alcance del asistente: el club y su administración (socios, altas y bajas, grupo familiar, tipos de socio, cuotas, liquidaciones, pagos, recibos, deudas, cobradores, reportes, usuarios y roles), las actividades, servicios, profesores, turnos y alquiler de espacios del club, los medios de pago (Red Link, Mercado Pago), la credencial virtual, el envío de avisos y recibos por correo o WhatsApp, el Sistema de Socios Web y la empresa 4x.

Tu tarea es responder exclusivamente utilizando el contexto proporcionado.

Reglas:
- Podés sintetizar, reformular o combinar datos del contexto para responder, aunque la pregunta no use las mismas palabras exactas que el contexto. Por ejemplo, si el contexto describe cómo un socio accede a una función, eso responde una pregunta sobre cómo hacer esa función, aunque el contexto no use la palabra 'cómo'.
- No inventar datos que no estén en el contexto (nombres, números, plazos, precios, pasos que no se mencionan).
- No utilizar conocimiento externo al contexto.
- Priorizar precisión sobre cantidad.
- Si el mensaje es solo un saludo o un agradecimiento, respondé brevemente y con cordialidad, ofreciendo ayuda.

Cuando no puedas responder, tu respuesta completa debe ser únicamente una de estas dos marcas, sin ningún otro texto:
- [SIN_INFO]: la pregunta está relacionada con el alcance del asistente (o podría estarlo), pero después de leer todo el contexto la información pedida no está ahí, ni siquiera de forma indirecta.
- [FUERA_DE_TEMA]: la pregunta claramente no tiene ninguna relación con el alcance del asistente (por ejemplo: recetas de cocina, el clima, resultados de partidos profesionales, tareas escolares, cultura general) o es un texto sin sentido.
Ante la mínima duda entre las dos, usá [SIN_INFO].

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

        // 7. Interpretación de la respuesta: si no hubo respuesta, se muestra el mensaje
        //    estándar y, si la pregunta es del tema, se registra para el panel de admin.
        var outcome = InterpretModelOutput(result.ToString());

        if (outcome.ShouldRegister)
            await RegisterUnansweredQuestionAsync(userQuery, clubId);

        return outcome.Answer;
    }

    internal static AssistantOutcome InterpretModelOutput(string modelOutput)
    {
        var output = modelOutput?.Trim() ?? string.Empty;

        // Ante la duda se registra: [SIN_INFO] gana sobre [FUERA_DE_TEMA]. También se
        // registra si el modelo devolvió vacío o el mensaje estándar sin la marca, para
        // no perder una pregunta legítima por un formato inesperado.
        if (output.Length == 0
            || output.Contains(NoInformationMarker, StringComparison.OrdinalIgnoreCase)
            || output.Contains("no tengo información sobre ese tema", StringComparison.OrdinalIgnoreCase))
        {
            return new AssistantOutcome(NoInformationMessage, ShouldRegister: true);
        }

        if (output.Contains(OffTopicMarker, StringComparison.OrdinalIgnoreCase))
            return new AssistantOutcome(NoInformationMessage, ShouldRegister: false);

        return new AssistantOutcome(output, ShouldRegister: false);
    }

    private async Task RegisterUnansweredQuestionAsync(string userQuery, int? clubId)
    {
        try
        {
            // Sin el token del request: si el usuario cierra la pestaña igual queremos
            // guardar la pregunta. Un fallo acá nunca debe romper la respuesta del chat.
            await _unansweredQuestionRepository.AddAsync(
                UnansweredQuestion.Create(userQuery, clubId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar la pregunta sin respuesta");
        }
    }
}

internal record AssistantOutcome(string Answer, bool ShouldRegister);