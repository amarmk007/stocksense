using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StockSense.API.DTOs;
using StockSense.API.Models;

namespace StockSense.API.Services;

public class ClaudeService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<ClaudeService> logger)
{
    private const string Model = "claude-sonnet-4-6";

    private HttpClient CreateClient()
    {
        var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? config["AnthropicApiKey"]
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not set");

        var http = httpFactory.CreateClient();
        http.BaseAddress = new Uri("https://api.anthropic.com");
        http.DefaultRequestHeaders.Add("x-api-key", key);
        http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    private async Task<string> CallClaude(string system, string userPrompt, int maxTokens)
    {
        var http = CreateClient();

        var body = JsonSerializer.Serialize(new
        {
            model = Model,
            max_tokens = maxTokens,
            system,
            messages = new[] { new { role = "user", content = userPrompt } }
        });

        var response = await http.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Anthropic API error {(int)response.StatusCode}: {raw}");

        var doc = JsonDocument.Parse(raw);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }

    public async Task<List<string>> SelectTickersAsync(UserProfile profile)
    {
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

        var text = await CallClaude(
            "You are a stock research analyst. Only recommend US-listed NYSE/NASDAQ stocks. Return ONLY a JSON array.",
            prompt,
            200);

        logger.LogInformation("SelectTickers raw response: {Text}", text);

        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < 0)
        {
            logger.LogWarning("Could not parse tickers from response, using defaults");
            return ["AAPL", "MSFT", "NVDA", "GOOGL", "AMZN"];
        }

        return JsonSerializer.Deserialize<List<string>>(text[start..(end + 1)]) ?? [];
    }

    public async Task<List<RecommendationItemDto>> GenerateRecommendationsAsync(
        UserProfile profile,
        List<FinnhubTickerData> marketData,
        Dictionary<string, List<EdgarFiling>> filings)
    {
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

        var jsonSchema = """{"recommendations":[{"ticker":"AAPL","name":"Apple Inc.","upside_estimate":"+15%","reasoning":"...","signals":{"analyst":["..."],"macro":["..."],"market":["..."]},"sources":[{"title":"...","url":"..."}]}]}""";

        var prompt = $"""
            User profile: ${profile.InvestmentAmount:N0}, {profile.TimelineYears}yr timeline, {profile.ExpectedReturnPct}% target, {profile.ExperienceLevel}.
            {experiencePrompt}

            Market data:
            {marketSummary}

            Respond with ONLY a JSON object matching this schema (no markdown, no explanation):
            {jsonSchema}
            """;

        var text = await CallClaude(
            "You are a stock research analyst. Return ONLY valid JSON matching the requested format. No markdown, no explanation.",
            prompt,
            8000);

        logger.LogInformation("GenerateRecommendations raw response length: {Len}", text.Length);

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0)
        {
            logger.LogError("No JSON object found in Claude response: {Text}", text[..Math.Min(500, text.Length)]);
            return [];
        }

        var json = text[start..(end + 1)];
        var parsed = JsonDocument.Parse(json);
        var recs = parsed.RootElement.GetProperty("recommendations");

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
}
