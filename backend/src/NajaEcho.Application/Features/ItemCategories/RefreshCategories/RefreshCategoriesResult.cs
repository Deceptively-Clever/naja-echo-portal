namespace NajaEcho.Application.Features.ItemCategories.RefreshCategories;

public sealed record RefreshCategoriesResult(
    int Fetched,
    int Inserted,
    int Updated,
    int Unchanged,
    int Failed,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public long DurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;
}
