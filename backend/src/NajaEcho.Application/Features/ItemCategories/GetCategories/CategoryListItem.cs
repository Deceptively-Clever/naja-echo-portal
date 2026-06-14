namespace NajaEcho.Application.Features.ItemCategories.GetCategories;

public sealed record CategoryListItem(
    int UexId,
    string Name,
    string? Section,
    string Type,
    bool IsGameRelated,
    bool IsMining,
    DateTimeOffset? SourceDateModified,
    int LocalItemCount,
    DateTimeOffset? LastImportedAt);
