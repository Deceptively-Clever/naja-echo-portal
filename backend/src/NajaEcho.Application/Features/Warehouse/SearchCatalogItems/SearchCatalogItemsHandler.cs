using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.SearchCatalogItems;

public sealed class SearchCatalogItemsHandler(
    IWarehouseInventoryRepository repository,
    ILogger<SearchCatalogItemsHandler> logger)
{
    public async Task<IReadOnlyList<CatalogItemResultDto>> HandleAsync(SearchCatalogItemsQuery query, CancellationToken ct)
    {
        logger.LogInformation("SearchCatalogItems search={Search} limit={Limit}", query.Search, query.Limit);
        var results = await repository.SearchCatalogItemsAsync(query.Search, query.Limit, ct);
        logger.LogInformation("SearchCatalogItems returned {Count} results", results.Count);
        return results;
    }
}
