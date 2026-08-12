using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("UploadPolicy")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] int? clubId, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No se proporcionó ningún archivo.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest("El archivo supera el tamaño máximo permitido (10 MB).");
        }

        using var stream = file.OpenReadStream();

        var chunksStored = await _documentService.ProcessAndStoreDocumentAsync(file.FileName, stream, file.ContentType, clubId, cancellationToken);

        if (chunksStored > 0)
        {
            return Ok(new { message = "Documento procesado correctamente.", chunksStored });
        }

        return StatusCode(500, "Ocurrió un error al procesar el documento. Asegúrate de que sea un PDF o un .txt válido.");
    }
}