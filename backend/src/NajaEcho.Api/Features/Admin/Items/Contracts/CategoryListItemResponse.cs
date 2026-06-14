namespace NajaEcho.Api.Features.Admin.Items.Contracts;

public sealed record CategoryListItemResponse(
    int UexId,
    string Name,
    string? Section,
    string Type,
    bool IsGameRelated,
    bool IsMining,
    DateTimeOffset? SourceDateModified,
    int LocalItemCount,
    DateTimeOffset? LastImportedAt);
