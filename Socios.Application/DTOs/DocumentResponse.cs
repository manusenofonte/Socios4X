namespace Socios.Application.DTOs;

public class DocumentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int? ClubId { get; set; }
    public int ChunkCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
}
