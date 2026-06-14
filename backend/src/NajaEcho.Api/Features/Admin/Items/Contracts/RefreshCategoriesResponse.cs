namespace NajaEcho.Api.Features.Admin.Items.Contracts;

public sealed record RefreshCategoriesResponse(
    int Fetched,
    int Inserted,
    int Updated,
    int Unchanged,
    int Failed,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMs);
