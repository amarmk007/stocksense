namespace StockSense.API.DTOs;

public record ProfileDto(
    int InvestmentAmount,
    int TimelineYears,
    int ExpectedReturnPct,
    string ExperienceLevel
);

public record PatchProfileDto(
    int? InvestmentAmount,
    int? TimelineYears,
    int? ExpectedReturnPct,
    string? ExperienceLevel
);
