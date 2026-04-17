using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.API.Data;
using StockSense.API.Models;
using StockSense.API.Services;

namespace StockSense.API.Jobs;

public class DailyRecommendationJob(
    AppDbContext db,
    ClaudeService claude,
    FinnhubService finnhub,
    EdgarService edgar,
    ILogger<DailyRecommendationJob> logger)
{
    public async Task ExecuteAsync()
    {
        var users = await db.Users
            .Include(u => u.Profile)
            .Where(u => u.IsOnboarded && u.Profile != null)
            .ToListAsync();

        logger.LogInformation("DailyRecommendationJob: processing {Count} users", users.Count);

        foreach (var user in users)
        {
            try
            {
                await ProcessUserAsync(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate recommendations for user {UserId}", user.Id);
            }
            await Task.Delay(1000);
        }
    }

    private async Task ProcessUserAsync(User user)
    {
        var profile = user.Profile!;

        // Pass 1: select tickers
        var tickers = await claude.SelectTickersAsync(profile);
        logger.LogInformation("Selected tickers for {UserId}: {Tickers}", user.Id, string.Join(", ", tickers));

        // Fetch market data in parallel (respecting Finnhub 60/min by limiting concurrency)
        var marketDataTasks = tickers.Select(t => finnhub.GetTickerDataAsync(t));
        var marketData = (await Task.WhenAll(marketDataTasks)).ToList();

        // Fetch EDGAR filings
        var filingsTasks = tickers.Select(async t => (t, await edgar.GetFilingsAsync(t)));
        var filingsArray = await Task.WhenAll(filingsTasks);
        var filings = filingsArray.ToDictionary(x => x.t, x => x.Item2);

        // Pass 2: generate recommendations
        var recommendations = await claude.GenerateRecommendationsAsync(profile, marketData, filings);

        if (recommendations.Count == 0)
        {
            logger.LogWarning("No recommendations generated for user {UserId}", user.Id);
            return;
        }

        // Mark previous sets stale
        var previous = await db.RecommendationSets
            .Where(s => s.UserId == user.Id && !s.IsStale)
            .ToListAsync();
        foreach (var s in previous) s.IsStale = true;

        // Save new set
        var set = new RecommendationSet { UserId = user.Id };
        db.RecommendationSets.Add(set);
        await db.SaveChangesAsync();

        var items = recommendations.Select((r, i) => new RecommendationItem
        {
            SetId = set.Id,
            Ticker = r.Ticker,
            Name = r.Name,
            UpsideEstimate = r.UpsideEstimate,
            Reasoning = r.Reasoning,
            SignalsJson = JsonSerializer.Serialize(r.Signals),
            SourcesJson = JsonSerializer.Serialize(r.Sources),
            SortOrder = i,
        });

        db.RecommendationItems.AddRange(items);
        await db.SaveChangesAsync();

        logger.LogInformation("Saved {Count} recommendations for user {UserId}", recommendations.Count, user.Id);
    }
}
