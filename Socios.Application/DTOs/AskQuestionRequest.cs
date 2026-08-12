using System.ComponentModel.DataAnnotations;

namespace Socios.Application.DTOs;

public class AskQuestionRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "La consulta debe tener entre 1 y 1000 caracteres.")]
    public string Query { get; set; } = string.Empty;

    public int? ClubId { get; set; } // Para manejar el multi-tenancy si es necesario
}