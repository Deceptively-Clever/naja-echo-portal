using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.ItemCategories;

namespace NajaEcho.Application.Features.ItemCategories.GetCategories;

public sealed class GetCategoriesHandler(
    IItemCategoryRepository repository,
    ILogger<GetCategoriesHandler> logger)
{
    public async Task<GetCategoriesResult> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
    {
        logger.LogInformation("GetCategories: fetching local categories");

        var eligible = await repository.GetEligibleAsync(ct);
        var lastRefreshedAt = await repository.GetLastRefreshedAtAsync(ct);

        var items = new List<CategoryListItem>(eligible.Count);
        foreach (var cat in eligible)
        {
            var itemCount = await repository.GetActiveItemCountAsync(cat.UexId, ct);
            var lastImportedAt = await repository.GetLastImportedAtAsync(cat.UexId, ct);
            items.Add(new CategoryListItem(
                cat.UexId,
                cat.Name,
                cat.Section,
                cat.Type,
                cat.IsGameRelated,
                cat.IsMining,
                cat.SourceDateModified,
                itemCount,
                lastImportedAt));
        }

        logger.LogInformation("GetCategories: returned {Count} eligible categories", items.Count);
        return new GetCategoriesResult(items, lastRefreshedAt);
    }
}
