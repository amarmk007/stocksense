namespace StockSense.API.Models;

public class RecommendationSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsStale { get; set; } = false;

    public List<RecommendationItem> Items { get; set; } = [];
}
