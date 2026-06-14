namespace NajaEcho.Application.Features.Commodities.ImportCommodities;

public sealed record ImportCommoditiesResult(
    int Fetched,
    int Skipped,
    int Inserted,
    int Updated,
    int Restored,
    int SoftDeleted,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMs,
    string? Warning = null);
