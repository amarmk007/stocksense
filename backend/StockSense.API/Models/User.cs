using System.ComponentModel.DataAnnotations;

namespace StockSense.API.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(255)]
    public required string GoogleId { get; set; }

    public bool IsOnboarded { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile? Profile { get; set; }
}
