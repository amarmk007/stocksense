using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSense.API.Data;
using StockSense.API.DTOs;
using StockSense.API.Jobs;
using StockSense.API.Models;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(AppDbContext db) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (UserId is not Guid userId) return Unauthorized();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();
        return Ok(new ProfileDto(
            profile.InvestmentAmount,
            profile.TimelineYears,
            profile.ExpectedReturnPct,
            profile.ExperienceLevel));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProfileDto dto)
    {
        if (UserId is not Guid userId) return Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var existing = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing != null) return Conflict("Profile already exists. Use PATCH to update.");

        var profile = new UserProfile
        {
            UserId = userId,
            InvestmentAmount = dto.InvestmentAmount,
            TimelineYears = dto.TimelineYears,
            ExpectedReturnPct = dto.ExpectedReturnPct,
            ExperienceLevel = dto.ExperienceLevel,
            UpdatedAt = DateTime.UtcNow,
        };

        db.UserProfiles.Add(profile);
        user.IsOnboarded = true;
        await db.SaveChangesAsync();

        BackgroundJob.Enqueue<DailyRecommendationJob>(j => j.ExecuteAsync());

        return Ok(new ProfileDto(
            profile.InvestmentAmount,
            profile.TimelineYears,
            profile.ExpectedReturnPct,
            profile.ExperienceLevel));
    }

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] PatchProfileDto dto)
    {
        if (UserId is not Guid userId) return Unauthorized();

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        if (dto.InvestmentAmount.HasValue) profile.InvestmentAmount = dto.InvestmentAmount.Value;
        if (dto.TimelineYears.HasValue) profile.TimelineYears = dto.TimelineYears.Value;
        if (dto.ExpectedReturnPct.HasValue) profile.ExpectedReturnPct = dto.ExpectedReturnPct.Value;
        if (dto.ExperienceLevel != null) profile.ExperienceLevel = dto.ExperienceLevel;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new ProfileDto(
            profile.InvestmentAmount,
            profile.TimelineYears,
            profile.ExpectedReturnPct,
            profile.ExperienceLevel));
    }
}
