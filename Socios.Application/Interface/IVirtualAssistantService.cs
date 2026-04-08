namespace Socios.Application.Interfaces;

public interface IVirtualAssistantService
{
    // Ejecuta el flujo RAG: busca en FAQs -> busca en Vectores -> Genera respuesta
    Task<string> AskQuestionAsync(string userQuery, int? clubId, CancellationToken cancellationToken = default);
}