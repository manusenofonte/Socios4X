namespace Socios.Application.Interfaces;

public interface IVirtualAssistantService
{
    // Ejecuta el flujo RAG: busca en FAQs y en vectores (en paralelo) -> genera respuesta
    Task<string> AskQuestionAsync(string userQuery, int? clubId, CancellationToken cancellationToken = default);

    // Igual que AskQuestionAsync, pero devuelve la respuesta en partes a medida que el modelo la genera
    IAsyncEnumerable<string> AskQuestionStreamAsync(string userQuery, int? clubId, CancellationToken cancellationToken = default);
}
