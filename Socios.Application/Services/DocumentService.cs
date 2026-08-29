using Microsoft.Extensions.Logging;
using Socios.Application.Interfaces;
using Socios.Domain.Entities;
using UglyToad.PdfPig;
using System.Text;

namespace Socios.Application.Services;

public class DocumentService : IDocumentService
{
    private const int MaxChunkSize = 1000;

    private readonly IVectorKnowledgeRepository _vectorRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IVectorKnowledgeRepository vectorRepository, IDocumentRepository documentRepository, ILogger<DocumentService> logger)
    {
        _vectorRepository = vectorRepository;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<int> ProcessAndStoreDocumentAsync(string fileName, Stream fileStream, string contentType, int? clubId, CancellationToken cancellationToken = default)
    {
        if (contentType != "application/pdf" && contentType != "text/plain")
            return 0;

        var document = Document.Create(fileName, contentType, clubId);
        await _documentRepository.AddAsync(document, cancellationToken);

        try
        {
            var extractedText = contentType == "application/pdf"
                ? ExtractTextFromPdf(fileStream)
                : await ExtractTextFromPlainTextAsync(fileStream, cancellationToken);
            var chunks = ChunkText(extractedText);

            if (chunks.Count == 0)
            {
                document.MarkAsFailed();
                await _documentRepository.UpdateAsync(document, cancellationToken);
                return 0;
            }

            await _vectorRepository.UpsertAsync(document.Id, fileName, clubId, chunks, cancellationToken);

            document.MarkAsProcessed(chunks.Count);
            await _documentRepository.UpdateAsync(document, cancellationToken);

            return chunks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando el documento {FileName}", fileName);
            document.MarkAsFailed();
            await _documentRepository.UpdateAsync(document, cancellationToken);
            return 0;
        }
    }

    public Task<IEnumerable<Document>> GetDocumentsAsync(int? clubId, CancellationToken cancellationToken = default)
    {
        return _documentRepository.GetAllAsync(clubId, cancellationToken);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return false;

        await _vectorRepository.DeleteByDocumentIdAsync(documentId, cancellationToken);
        await _documentRepository.DeleteAsync(document, cancellationToken);

        return true;
    }

    public async Task<bool> UpdateDocumentAsync(int documentId, string fileName, int? clubId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return false;

        var fileNameChanged = document.FileName != fileName;

        document.UpdateMetadata(fileName, clubId);
        await _documentRepository.UpdateAsync(document, cancellationToken);

        if (fileNameChanged)
            await _vectorRepository.UpdateSourceAsync(documentId, fileName, cancellationToken);

        return true;
    }

    private static string ExtractTextFromPdf(Stream pdfStream)
    {
        var textBuilder = new StringBuilder();

        using (var document = PdfDocument.Open(pdfStream, new ParsingOptions { UseLenientParsing = true }))
        {
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
        }

        return textBuilder.ToString();
    }

    private static async Task<string> ExtractTextFromPlainTextAsync(Stream textStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(textStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static List<string> ChunkText(string text, int maxChunkSize = MaxChunkSize)
    {
        var chunks = new List<string>();
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (current.Length > 0 && current.Length + trimmed.Length > maxChunkSize)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }

            // Una línea sola ya más larga que el tamaño máximo: la partimos directamente
            if (trimmed.Length > maxChunkSize)
            {
                for (int i = 0; i < trimmed.Length; i += maxChunkSize)
                    chunks.Add(trimmed.Substring(i, Math.Min(maxChunkSize, trimmed.Length - i)));
                continue;
            }

            current.AppendLine(trimmed);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }
}
