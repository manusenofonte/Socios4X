using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;

namespace Socios.Application.Services;

public class VirtualAssistantService : IVirtualAssistantService
{
    public const string NoInformationMessage =
        "Lo siento, no tengo información sobre ese tema. Te recomiendo contactarte con la administración del club o 4x.";

    // Marcas que el modelo devuelve cuando no puede responder (ver prompt)
    internal const string NoInformationMarker = "[SIN_INFO]";
    internal const string OffTopicMarker = "[FUERA_DE_TEMA]";

    private const int MaxContextChunks = 6;

    private const string PromptTemplate = @"
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

    private readonly Kernel _kernel;
    private readonly IFAQRepository _faqRepository;
    private readonly IVectorKnowledgeRepository _vectorRepository;
    private readonly IUnansweredQuestionRepository _unansweredQuestionRepository;
    private readonly AssistantOptions _options;
    private readonly ILogger<VirtualAssistantService> _logger;

    public VirtualAssistantService(
        Kernel kernel,
        IFAQRepository faqRepository,
        IVectorKnowledgeRepository vectorRepository,
        IUnansweredQuestionRepository unansweredQuestionRepository,
        IOptions<AssistantOptions> options,
        ILogger<VirtualAssistantService> logger)
    {
        _kernel = kernel;
        _faqRepository = faqRepository;
        _vectorRepository = vectorRepository;
        _unansweredQuestionRepository = unansweredQuestionRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> AskQuestionAsync(
        string userQuery,
        int? clubId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("La consulta no puede estar vacía.");

        var stopwatch = Stopwatch.StartNew();
        var arguments = await BuildPromptArgumentsAsync(userQuery, clubId, cancellationToken);
        var retrievalMs = stopwatch.ElapsedMilliseconds;

        var result = await _kernel.InvokePromptAsync(PromptTemplate, arguments, cancellationToken: cancellationToken);

        // Si no hubo respuesta, se muestra el mensaje estándar y, si la pregunta es
        // del tema, se registra para el panel de admin.
        var outcome = InterpretModelOutput(result.ToString());

        if (outcome.ShouldRegister)
            await RegisterUnansweredQuestionAsync(userQuery, clubId);

        _logger.LogInformation(
            "Consulta respondida: búsqueda {RetrievalMs} ms, total {TotalMs} ms",
            retrievalMs, stopwatch.ElapsedMilliseconds);

        return outcome.Answer;
    }

    public async IAsyncEnumerable<string> AskQuestionStreamAsync(
        string userQuery,
        int? clubId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("La consulta no puede estar vacía.");

        var stopwatch = Stopwatch.StartNew();
        var arguments = await BuildPromptArgumentsAsync(userQuery, clubId, cancellationToken);
        var retrievalMs = stopwatch.ElapsedMilliseconds;
        long? firstTextMs = null;

        var fullOutput = new StringBuilder();
        var streamingToUser = false;

        await foreach (var update in _kernel.InvokePromptStreamingAsync(PromptTemplate, arguments, cancellationToken: cancellationToken))
        {
            var text = update.ToString();
            if (string.IsNullOrEmpty(text))
                continue;

            fullOutput.Append(text);

            if (streamingToUser)
            {
                yield return text;
                continue;
            }

            // Todavía no se mandó nada: si la respuesta es una marca ([SIN_INFO] o
            // [FUERA_DE_TEMA]) no tiene que llegar al usuario, así que retenemos el
            // texto hasta poder decidir.
            var decision = DecideStreamStart(fullOutput.ToString());
            if (decision == StreamStart.Answer)
            {
                streamingToUser = true;
                firstTextMs = stopwatch.ElapsedMilliseconds;
                yield return fullOutput.ToString().TrimStart();
            }
        }

        var outcome = InterpretModelOutput(fullOutput.ToString());

        // Era una marca (o vino vacío): recién ahora se manda el mensaje estándar
        if (!streamingToUser)
            yield return outcome.Answer;

        if (outcome.ShouldRegister)
            await RegisterUnansweredQuestionAsync(userQuery, clubId);

        _logger.LogInformation(
            "Consulta respondida (stream): búsqueda {RetrievalMs} ms, primer texto {FirstTextMs} ms, total {TotalMs} ms",
            retrievalMs, firstTextMs, stopwatch.ElapsedMilliseconds);
    }

    private async Task<KernelArguments> BuildPromptArgumentsAsync(string userQuery, int? clubId, CancellationToken cancellationToken)
    {
        // Las dos búsquedas son independientes (SQL y Qdrant): corren en paralelo
        var faqsTask = _faqRepository.SearchRelevantFAQsAsync(userQuery, cancellationToken);
        var documentChunksTask = _vectorRepository.SearchAsync(userQuery, clubId, cancellationToken);
        await Task.WhenAll(faqsTask, documentChunksTask);

        // Normalización de FAQs como chunks (score real de coincidencia, no fijo)
        var faqChunks = faqsTask.Result.Select(m => new KnowledgeChunk
        {
            Text = $"Pregunta: {m.Faq.Question}\nRespuesta: {m.Faq.Answer}",
            Score = m.Score,
            Source = "FAQ"
        });

        // Unificación y ordenamiento por relevancia
        var allChunks = faqChunks
            .Concat(documentChunksTask.Result)
            .OrderByDescending(c => c.Score)
            .Take(MaxContextChunks)
            .ToList();

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("--- CONTEXTO RELEVANTE ---");

        if (allChunks.Count > 0)
        {
            foreach (var chunk in allChunks)
                contextBuilder.AppendLine($"[{chunk.Source}] {chunk.Text}");
        }
        else
        {
            contextBuilder.AppendLine("No hay información relevante.");
        }

        contextBuilder.AppendLine("--- FIN CONTEXTO ---");

        var arguments = _options.ExecutionSettings is null
            ? new KernelArguments()
            : new KernelArguments(_options.ExecutionSettings);

        arguments["context"] = contextBuilder.ToString();
        arguments["query"] = userQuery;

        return arguments;
    }

    // Decide, con el texto recibido hasta ahora, si ya se puede empezar a mandar al usuario
    internal static StreamStart DecideStreamStart(string outputSoFar)
    {
        var trimmed = outputSoFar.TrimStart();
        if (trimmed.Length == 0)
            return StreamStart.Undecided;

        // Las marcas empiezan con "[": cualquier otra cosa es una respuesta normal
        if (trimmed[0] != '[')
            return StreamStart.Answer;

        var longestMarker = Math.Max(NoInformationMarker.Length, OffTopicMarker.Length);
        if (!trimmed.Contains(']') && trimmed.Length < longestMarker)
            return StreamStart.Undecided;

        return trimmed.StartsWith(NoInformationMarker, StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(OffTopicMarker, StringComparison.OrdinalIgnoreCase)
                ? StreamStart.Marker
                : StreamStart.Answer; // p. ej. un link de Markdown "[texto](url)"
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

internal enum StreamStart
{
    Undecided,
    Marker,
    Answer
}
