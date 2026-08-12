using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AssistantPolicy")]
public class AssistantController : ControllerBase
{
    private readonly IVirtualAssistantService _assistantService;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(IVirtualAssistantService assistantService, ILogger<AssistantController> logger)
    {
        _assistantService = assistantService;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskQuestionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { Error = "La consulta no puede estar vacía." });
        }

        try
        {
            var response = await _assistantService.AskQuestionAsync(request.Query, request.ClubId, cancellationToken);
            return Ok(new { Answer = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando la consulta del asistente");
            return StatusCode(500, new { Error = "Ocurrió un error interno procesando la consulta." });
        }
    }
}