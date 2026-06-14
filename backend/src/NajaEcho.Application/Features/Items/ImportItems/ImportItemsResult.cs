namespace NajaEcho.Application.Features.Items.ImportItems;

public sealed record ImportItemsResult(
    ImportStatus Status,
    int CategoriesProcessed,
    int CategoriesSucceeded,
    int CategoriesFailed,
    int ItemsFetched,
    int ItemsInserted,
    int ItemsUpdated,
    int ItemsUnchanged,
    int ItemsSkippedNoUuid,
    int ItemsSoftDeleted,
    int ItemsFailed,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<CategoryImportError> Errors)
{
    public long DurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;
}

public enum ImportStatus
{
    Success,
    CompletedWithErrors,
    Failed,
}
