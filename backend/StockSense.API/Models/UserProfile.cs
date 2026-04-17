namespace StockSense.API.Models;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int InvestmentAmount { get; set; }
    public int TimelineYears { get; set; }
    public int ExpectedReturnPct { get; set; }
    public string ExperienceLevel { get; set; } = "Novice";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
