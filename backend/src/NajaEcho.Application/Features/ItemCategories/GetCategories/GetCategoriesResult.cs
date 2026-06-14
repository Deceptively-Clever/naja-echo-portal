namespace NajaEcho.Application.Features.ItemCategories.GetCategories;

public sealed record GetCategoriesResult(
    IReadOnlyList<CategoryListItem> Items,
    DateTimeOffset? LastRefreshedAt);
