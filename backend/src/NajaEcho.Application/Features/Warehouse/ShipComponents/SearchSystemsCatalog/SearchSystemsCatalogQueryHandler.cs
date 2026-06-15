using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;

public sealed class SearchSystemsCatalogQueryHandler(
    IShipComponentRepository repository,
    ILogger<SearchSystemsCatalogQueryHandler> logger)
{
    public async Task<IReadOnlyList<SystemsCatalogItemDto>> HandleAsync(SearchSystemsCatalogQuery query, CancellationToken ct)
    {
        logger.LogInformation("SearchSystemsCatalog search={Search} limit={Limit}", query.Search, query.Limit);
        var results = await repository.SearchSystemsCatalogAsync(query.Search, Math.Clamp(query.Limit, 1, 100), ct);
        logger.LogInformation("SearchSystemsCatalog returned {Count} items", results.Count);
        return results;
    }
}
