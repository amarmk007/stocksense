using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockSense.API.Services;

public class FinnhubQuote
{
    [JsonPropertyName("c")] public decimal CurrentPrice { get; set; }
    [JsonPropertyName("h")] public decimal High { get; set; }
    [JsonPropertyName("l")] public decimal Low { get; set; }
    [JsonPropertyName("pc")] public decimal PreviousClose { get; set; }
}

public class FinnhubNewsItem
{
    [JsonPropertyName("headline")] public string Headline { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("datetime")] public long Datetime { get; set; }
}

public class FinnhubRating
{
    [JsonPropertyName("buy")] public int Buy { get; set; }
    [JsonPropertyName("hold")] public int Hold { get; set; }
    [JsonPropertyName("sell")] public int Sell { get; set; }
    [JsonPropertyName("strongBuy")] public int StrongBuy { get; set; }
    [JsonPropertyName("strongSell")] public int StrongSell { get; set; }
    [JsonPropertyName("period")] public string Period { get; set; } = "";
}

public class FinnhubTickerData
{
    public string Ticker { get; set; } = "";
    public FinnhubQuote Quote { get; set; } = new();
    public List<FinnhubNewsItem> News { get; set; } = [];
    public List<FinnhubRating> Ratings { get; set; } = [];
}

public class FinnhubService(IHttpClientFactory httpFactory, IConfiguration config)
{
    private readonly string _apiKey =
        Environment.GetEnvironmentVariable("FINNHUB_API_KEY")
        ?? config["FinnhubApiKey"]
        ?? "";

    public async Task<FinnhubTickerData> GetTickerDataAsync(string ticker)
    {
        using var http = httpFactory.CreateClient("finnhub");
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        var quoteTask = http.GetAsync($"/api/v1/quote?symbol={ticker}&token={_apiKey}");
        var newsTask = http.GetAsync($"/api/v1/company-news?symbol={ticker}&from={yesterday}&to={today}&token={_apiKey}");
        var ratingsTask = http.GetAsync($"/api/v1/stock/recommendation?symbol={ticker}&token={_apiKey}");

        await Task.WhenAll(quoteTask, newsTask, ratingsTask);

        var quote = await Deserialize<FinnhubQuote>(await quoteTask) ?? new();
        var news = await Deserialize<List<FinnhubNewsItem>>(await newsTask) ?? [];
        var ratings = await Deserialize<List<FinnhubRating>>(await ratingsTask) ?? [];

        return new FinnhubTickerData
        {
            Ticker = ticker,
            Quote = quote,
            News = news.Take(5).ToList(),
            Ratings = ratings.Take(3).ToList(),
        };
    }

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}
