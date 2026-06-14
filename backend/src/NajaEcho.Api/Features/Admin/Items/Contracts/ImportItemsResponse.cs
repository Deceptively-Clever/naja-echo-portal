namespace NajaEcho.Api.Features.Admin.Items.Contracts;

public sealed record ImportItemsResponse(
    string Status,
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
    long DurationMs,
    IReadOnlyList<CategoryImportErrorResponse> Errors);

public sealed record CategoryImportErrorResponse(int CategoryUexId, string? CategoryName, string Message);
