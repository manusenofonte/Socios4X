namespace Socios.Domain.Entities;

// Pregunta del chat que el asistente no pudo responder con la información cargada.
// Se lista en el panel de administración para detectar qué documentación falta.
public class UnansweredQuestion
{
    public const int MaxQuestionLength = 1000;

    public int Id { get; private set; }

    public string Question { get; private set; } = string.Empty;

    public int? ClubId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Constructor privado para EF Core
    private UnansweredQuestion() { }

    public static UnansweredQuestion Create(string question, int? clubId)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be empty");

        var trimmed = question.Trim();

        return new UnansweredQuestion
        {
            Question = trimmed.Length > MaxQuestionLength ? trimmed[..MaxQuestionLength] : trimmed,
            ClubId = clubId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
