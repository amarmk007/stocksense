namespace StockSense.API.DTOs;

public record SignalsDto(
    List<string> Analyst,
    List<string> Macro,
    List<string> Market
);

public record SourceDto(string Title, string Url);

public record RecommendationItemDto(
    string Ticker,
    string Name,
    string UpsideEstimate,
    string Reasoning,
    SignalsDto Signals,
    List<SourceDto> Sources
);

public record RecommendationResponse(
    bool IsStale,
    DateTime GeneratedAt,
    List<RecommendationItemDto> Recommendations
);
