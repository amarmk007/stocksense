using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockSense.API.Services;

public class EdgarFiling
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}

public class EdgarService(IHttpClientFactory httpFactory)
{
    public async Task<List<EdgarFiling>> GetFilingsAsync(string ticker)
    {
        using var http = httpFactory.CreateClient("edgar");

        var startdt = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var enddt = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var url = $"/LATEST/search-index?q=%22{Uri.EscapeDataString(ticker)}%22&dateRange=custom&startdt={startdt}&enddt={enddt}&forms=8-K,10-K";

        try
        {
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EdgarSearchResult>(json);

            return result?.Hits?.Hits?
                .Take(5)
                .Select(h => new EdgarFiling
                {
                    Title = h.Source?.DisplayNames ?? h.Source?.FormType ?? "SEC Filing",
                    Url = $"https://www.sec.gov/Archives/edgar/data/{h.Source?.EntityId}/{h.Source?.FileName}",
                })
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }
}

file class EdgarSearchResult
{
    [JsonPropertyName("hits")] public EdgarHitsWrapper? Hits { get; set; }
}

file class EdgarHitsWrapper
{
    [JsonPropertyName("hits")] public List<EdgarHit>? Hits { get; set; }
}

file class EdgarHit
{
    [JsonPropertyName("_source")] public EdgarSource? Source { get; set; }
}

file class EdgarSource
{
    [JsonPropertyName("display_names")] public string? DisplayNames { get; set; }
    [JsonPropertyName("form_type")] public string? FormType { get; set; }
    [JsonPropertyName("entity_id")] public string? EntityId { get; set; }
    [JsonPropertyName("file_name")] public string? FileName { get; set; }
}
