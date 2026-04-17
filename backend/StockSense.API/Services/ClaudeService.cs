using System.Text.Json;
using Anthropic;
using StockSense.API.DTOs;
using StockSense.API.Models;

namespace StockSense.API.Services;

public class ClaudeService(IConfiguration config)
{
    private AnthropicApi CreateClient()
    {
        var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? config["AnthropicApiKey"]
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not set");
        var api = new AnthropicApi();
        api.AuthorizeUsingApiKey(key);
        api.SetHeaders();
        return api;
    }

    public async Task<List<string>> SelectTickersAsync(UserProfile profile)
    {
        var api = CreateClient();

        var prompt = $"""
            User profile:
            - Investment amount: ${profile.InvestmentAmount:N0}
            - Timeline: {profile.TimelineYears} year(s)
            - Expected return: {profile.ExpectedReturnPct}%
            - Experience level: {profile.ExperienceLevel}
            Today: {DateTime.UtcNow:yyyy-MM-dd}

            Return ONLY a JSON array of 5-8 US-listed stock tickers (NYSE/NASDAQ). No explanation.
            Example: ["AAPL","MSFT","NVDA"]
            """;

        var response = await api.CreateMessageAsync(
            model: "claude-sonnet-4-6",
            messages: [prompt],
            maxTokens: 200,
            system: "You are a stock research analyst. Only recommend US-listed NYSE/NASDAQ stocks. Return ONLY a JSON array.");

        var text = ExtractText(response);

        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < 0) return ["AAPL", "MSFT", "NVDA", "GOOGL", "AMZN"];

        try { return JsonSerializer.Deserialize<List<string>>(text[start..(end + 1)]) ?? []; }
        catch { return ["AAPL", "MSFT", "NVDA", "GOOGL", "AMZN"]; }
    }

    public async Task<List<RecommendationItemDto>> GenerateRecommendationsAsync(
        UserProfile profile,
        List<FinnhubTickerData> marketData,
        Dictionary<string, List<EdgarFiling>> filings)
    {
        var api = CreateClient();

        var experiencePrompt = profile.ExperienceLevel == "Experienced"
            ? "Use precise financial terminology. Include data density — P/E ratios, price targets, volume context."
            : "Explain each signal in plain English. Avoid jargon. Prioritize the 'why this matters to me' angle.";

        var marketSummary = string.Join("\n\n", marketData.Select(d =>
        {
            var r = d.Ratings.FirstOrDefault();
            var ratingSummary = r != null
                ? $"Analyst ratings: {r.StrongBuy} strong buy, {r.Buy} buy, {r.Hold} hold, {r.Sell} sell"
                : "No analyst ratings";
            var newsSummary = d.News.Any()
                ? string.Join("; ", d.News.Take(3).Select(n => n.Headline))
                : "No recent news";
            var edgarSummary = filings.TryGetValue(d.Ticker, out var f) && f.Any()
                ? string.Join("; ", f.Take(3).Select(x => x.Title))
                : "No recent filings";
            return $"{d.Ticker}: price=${d.Quote.CurrentPrice:F2} | {ratingSummary} | news: {newsSummary} | SEC: {edgarSummary}";
        }));

        var jsonSchema = """
            {"recommendations":[{"ticker":"AAPL","name":"Apple Inc.","upside_estimate":"+15%","reasoning":"...","signals":{"analyst":["..."],"macro":["..."],"market":["..."]},"sources":[{"title":"...","url":"..."}]}]}
            """;

        var prompt = $"""
            User profile: ${profile.InvestmentAmount:N0}, {profile.TimelineYears}yr timeline, {profile.ExpectedReturnPct}% target, {profile.ExperienceLevel}.
            {experiencePrompt}

            Market data:
            {marketSummary}

            Respond with ONLY a JSON object matching this schema (no markdown, no explanation):
            {jsonSchema}
            """;

        var response = await api.CreateMessageAsync(
            model: "claude-sonnet-4-6",
            messages: [prompt],
            maxTokens: 8000,
            system: "You are a stock research analyst. Return ONLY valid JSON matching the requested format. No markdown, no explanation.");

        var text = ExtractText(response);

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0) return [];

        try
        {
            var json = text[start..(end + 1)];
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            var recs = parsed.GetProperty("recommendations");

            return recs.EnumerateArray().Select(r =>
            {
                var signals = r.GetProperty("signals");
                return new RecommendationItemDto(
                    r.GetProperty("ticker").GetString() ?? "",
                    r.GetProperty("name").GetString() ?? "",
                    r.GetProperty("upside_estimate").GetString() ?? "",
                    r.GetProperty("reasoning").GetString() ?? "",
                    new SignalsDto(
                        signals.GetProperty("analyst").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                        signals.GetProperty("macro").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                        signals.GetProperty("market").EnumerateArray().Select(x => x.GetString() ?? "").ToList()),
                    r.GetProperty("sources").EnumerateArray().Select(s => new SourceDto(
                        s.GetProperty("title").GetString() ?? "",
                        s.GetProperty("url").GetString() ?? "")).ToList());
            }).ToList();
        }
        catch { return []; }
    }

    private static string ExtractText(Anthropic.Message response)
    {
        if (response.Content.IsValue1)
            return response.Content.Value1 ?? "";
        var blocks = response.Content.Value2;
        if (blocks == null) return "";
        foreach (var block in blocks)
            if (block.IsText) return block.Text?.Text ?? "";
        return "";
    }
}
