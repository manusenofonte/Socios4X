using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Socios.Api.Filters;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/unanswered-questions")]
[RequireAdminKey]
[EnableRateLimiting("AdminPolicy")]
public class UnansweredQuestionsController : ControllerBase
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IUnansweredQuestionRepository _repository;

    public UnansweredQuestionsController(IUnansweredQuestionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var questions = await _repository.GetPagedAsync(page, pageSize, cancellationToken);

        var response = new PagedResult<UnansweredQuestionResponse>
        {
            Items = questions.Items.Select(q => new UnansweredQuestionResponse
            {
                Id = q.Id,
                Question = q.Question,
                ClubId = q.ClubId,
                CreatedAt = q.CreatedAt
            }).ToList(),
            Page = questions.Page,
            PageSize = questions.PageSize,
            TotalCount = questions.TotalCount
        };

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { Error = "No se encontró la pregunta." });
        }

        return NoContent();
    }
}
