namespace NajaEcho.Api.Features.Admin.Items.Contracts;

public sealed record CategoryListResponse(
    IReadOnlyList<CategoryListItemResponse> Categories,
    DateTimeOffset? LastRefreshedAt);
