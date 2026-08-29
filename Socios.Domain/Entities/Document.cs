namespace Socios.Domain.Entities;

public class Document
{
    // Id
    public int Id { get; private set; }

    // NOT NULL en DB. Usamos string para C#
    public string FileName { get; private set; } = string.Empty;

    public string FileType { get; private set; } = string.Empty;

    public int? ClubId { get; private set; }

    public int ChunkCount { get; private set; }

    public DateTime UploadDate { get; private set; }

    // NULL permitido si aún no se procesó
    public DateTime? ProcessedDate { get; private set; }

    public string Status { get; private set; } = "Pending";

    // Constructor privado para EF Core
    private Document() { }

    public static Document Create(string fileName, string fileType, int? clubId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty");

        return new Document
        {
            FileName = fileName,
            FileType = fileType,
            ClubId = clubId,
            UploadDate = DateTime.UtcNow,
            Status = "Pending"
        };
    }

    public void MarkAsProcessed(int chunkCount)
    {
        Status = "Processed";
        ChunkCount = chunkCount;
        ProcessedDate = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
    }

    public void UpdateMetadata(string fileName, int? clubId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty");

        FileName = fileName;
        ClubId = clubId;
    }
}
