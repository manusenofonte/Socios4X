namespace Socios.Application.DTOs;

public class UnansweredQuestionResponse
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public int? ClubId { get; set; }
    public DateTime CreatedAt { get; set; }
}
