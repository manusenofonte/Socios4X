using Microsoft.AspNetCore.Mvc;
using Socios.Application.DTOs;
using Socios.Application.Interfaces;

namespace Socios.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaqController : ControllerBase
{
    private readonly IFAQRepository _faqRepository;

    public FaqController(IFAQRepository faqRepository)
    {
        _faqRepository = faqRepository;
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int count, CancellationToken cancellationToken)
    {
        var take = count <= 0 ? 5 : Math.Min(count, 20);

        var faqs = await _faqRepository.GetMostFrequentAsync(take, cancellationToken);

        var suggestions = faqs.Select(f => new FaqSuggestionResponse
        {
            Id = f.Id,
            Question = f.Question ?? string.Empty
        });

        return Ok(suggestions);
    }
}
