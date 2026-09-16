using Microsoft.SemanticKernel;

namespace Socios.Application.Services;

public class AssistantOptions
{
    // Configuración del modelo para cada consulta (la arma la API según el proveedor).
    // null = los valores por defecto del modelo.
    public PromptExecutionSettings? ExecutionSettings { get; set; }
}
