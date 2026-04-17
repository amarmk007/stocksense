using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.API.Data;
using StockSense.API.DTOs;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController(AppDbContext db) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (UserId is not Guid userId) return Unauthorized();

        var set = await db.RecommendationSets
            .Include(s => s.Items)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync();

        if (set == null) return NotFound();

        var items = set.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => new RecommendationItemDto(
                i.Ticker,
                i.Name,
                i.UpsideEstimate,
                i.Reasoning,
                System.Text.Json.JsonSerializer.Deserialize<SignalsDto>(i.SignalsJson)!,
                System.Text.Json.JsonSerializer.Deserialize<List<SourceDto>>(i.SourcesJson)!))
            .ToList();

        return Ok(new RecommendationResponse(set.IsStale, set.GeneratedAt, items));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        if (UserId is not Guid userId) return Unauthorized();

        var hasReady = await db.RecommendationSets
            .AnyAsync(s => s.UserId == userId && !s.IsStale);

        return Ok(new { status = hasReady ? "ready" : "pending" });
    }
}
