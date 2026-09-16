using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Api.Auth;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AdminPolicies.Panel)]
[EnableRateLimiting("AdminPolicy")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [EnableRateLimiting("UploadPolicy")]
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

    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int? clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var documents = await _documentService.GetDocumentsAsync(clubId, page, pageSize, cancellationToken);

        var response = new PagedResult<DocumentResponse>
        {
            Items = documents.Items.Select(d => new DocumentResponse
            {
                Id = d.Id,
                FileName = d.FileName,
                FileType = d.FileType,
                ClubId = d.ClubId,
                ChunkCount = d.ChunkCount,
                Status = d.Status,
                UploadDate = d.UploadDate,
                ProcessedDate = d.ProcessedDate
            }).ToList(),
            Page = documents.Page,
            PageSize = documents.PageSize,
            TotalCount = documents.TotalCount
        };

        return Ok(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentRequest request, CancellationToken cancellationToken)
    {
        var updated = await _documentService.UpdateDocumentAsync(id, request.FileName, request.ClubId, cancellationToken);

        if (!updated)
        {
            return NotFound(new { Error = "No se encontró el documento." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDocument(int id, CancellationToken cancellationToken)
    {
        var deleted = await _documentService.DeleteDocumentAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { Error = "No se encontró el documento." });
        }

        return NoContent();
    }
}
