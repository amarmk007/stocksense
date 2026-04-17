using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSense.API.Models;

public class RecommendationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SetId { get; set; }

    [MaxLength(10)]
    public required string Ticker { get; set; }

    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(20)]
    public required string UpsideEstimate { get; set; }

    public required string Reasoning { get; set; }

    [Column(TypeName = "jsonb")]
    public required string SignalsJson { get; set; }

    [Column(TypeName = "jsonb")]
    public required string SourcesJson { get; set; }

    public int SortOrder { get; set; }

    public RecommendationSet Set { get; set; } = null!;
}
