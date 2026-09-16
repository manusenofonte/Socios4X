using System.Text.Json;
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

    // Igual que /ask, pero la respuesta llega en partes a medida que la IA la genera
    // (Server-Sent Events): eventos "delta" con { text }, y al final "done" o "error".
    [HttpPost("ask/stream")]
    public async Task AskStream([FromBody] AskQuestionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { Error = "La consulta no puede estar vacía." }, cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // que ningún proxy junte los eventos

        try
        {
            await foreach (var text in _assistantService.AskQuestionStreamAsync(request.Query, request.ClubId, cancellationToken))
            {
                await WriteEventAsync("delta", new { text }, cancellationToken);
            }

            await WriteEventAsync("done", new { }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // El usuario cerró la pestaña o cortó la conexión: no hay a quién avisarle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando la consulta del asistente (stream)");

            // Si todavía no se mandó nada se puede responder un 500 normal
            if (!Response.HasStarted)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsJsonAsync(new { Error = "Ocurrió un error interno procesando la consulta." }, CancellationToken.None);
                return;
            }

            await WriteEventAsync("error", new { error = "Ocurrió un error interno procesando la consulta." }, CancellationToken.None);
        }
    }

    private async Task WriteEventAsync(string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}