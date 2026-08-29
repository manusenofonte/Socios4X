using System.ComponentModel.DataAnnotations;

namespace Socios.Application.DTOs;

public class UpdateDocumentRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    public int? ClubId { get; set; }
}
