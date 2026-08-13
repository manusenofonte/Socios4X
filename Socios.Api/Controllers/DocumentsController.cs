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
    private const string AdminKeyHeader = "X-Admin-Key";

    private readonly IDocumentService _documentService;
    private readonly IConfiguration _configuration;

    public DocumentsController(IDocumentService documentService, IConfiguration configuration)
    {
        _documentService = documentService;
        _configuration = configuration;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] int? clubId, CancellationToken cancellationToken)
    {
        var adminKey = _configuration["Admin:UploadKey"];
        var providedKey = Request.Headers[AdminKeyHeader].ToString();

        if (string.IsNullOrEmpty(adminKey) || providedKey != adminKey)
        {
            return Unauthorized("Se requiere una clave de administrador para cargar documentos.");
        }

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