using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;

public sealed class SearchCommoditiesQueryHandler(
    IMaterialInventoryRepository repository,
    ILogger<SearchCommoditiesQueryHandler> logger)
{
    public async Task<IReadOnlyList<CommodityResultDto>> HandleAsync(SearchCommoditiesQuery query, CancellationToken ct)
    {
        logger.LogInformation("SearchCommodities search={Search} limit={Limit}", query.Search, query.Limit);
        var results = await repository.SearchCommoditiesAsync(query.Search, query.Limit, ct);
        logger.LogInformation("SearchCommodities returned {Count} results", results.Count);
        return results;
    }
}
