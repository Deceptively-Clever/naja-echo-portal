namespace NajaEcho.Api.Features.Admin.Commodities.Contracts;

public sealed record ImportCommoditiesResponse(
    int Fetched,
    int Skipped,
    int Inserted,
    int Updated,
    int Unchanged,
    int Restored,
    int SoftDeleted,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMs,
    string? Warning = null);
