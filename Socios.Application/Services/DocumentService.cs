using Microsoft.Extensions.Logging;
using Socios.Application.Interfaces;
using UglyToad.PdfPig;
using System.Text;

namespace Socios.Application.Services;

public class DocumentService : IDocumentService
{
    private const int MaxChunkSize = 1000;

    private readonly IVectorKnowledgeRepository _vectorRepository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IVectorKnowledgeRepository vectorRepository, ILogger<DocumentService> logger)
    {
        _vectorRepository = vectorRepository;
        _logger = logger;
    }

    public async Task<int> ProcessAndStoreDocumentAsync(string fileName, Stream fileStream, string contentType, int? clubId, CancellationToken cancellationToken = default)
    {
        if (contentType != "application/pdf" && contentType != "text/plain")
            return 0;

        try
        {
            var extractedText = contentType == "application/pdf"
                ? ExtractTextFromPdf(fileStream)
                : await ExtractTextFromPlainTextAsync(fileStream, cancellationToken);
            var chunks = ChunkText(extractedText);

            if (chunks.Count == 0)
                return 0;

            await _vectorRepository.UpsertAsync(fileName, clubId, chunks, cancellationToken);

            return chunks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando el documento {FileName}", fileName);
            return 0;
        }
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
