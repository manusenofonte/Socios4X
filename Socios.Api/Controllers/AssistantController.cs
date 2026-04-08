using Microsoft.AspNetCore.Mvc;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/[controller]")] 
// La ruta será /api/assistant
public class AssistantController : ControllerBase
{
    private readonly IVirtualAssistantService _assistantService;

    // Inyectamos nuestro contrato de la capa de Aplicación
    public AssistantController(IVirtualAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskQuestionRequest request, CancellationToken cancellationToken)
    {
        // Validamos que no nos manden preguntas vacías
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { Error = "La consulta no puede estar vacía." });
        }

        try
        {
            // Ejecutamos el caso de uso RAG
            var response = await _assistantService.AskQuestionAsync(request.Query, request.ClubId, cancellationToken);

            // Devolvemos un 200 OK con la respuesta en formato JSON
            return Ok(new { Answer = response });
        }
        catch (Exception ex)
        {
            // En producción, aquí usaríamos un logger profesional como Serilog
            return StatusCode(500, new { Error = "Ocurrió un error interno procesando la consulta.", Details = ex.Message });
        }
    }
}