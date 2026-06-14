using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.GetInventoryFilters;

public sealed class GetInventoryFiltersHandler(
    IWarehouseInventoryRepository repository,
    ILogger<GetInventoryFiltersHandler> logger)
{
    public async Task<InventoryFiltersDto> HandleAsync(GetInventoryFiltersQuery query, CancellationToken ct)
    {
        logger.LogInformation("GetInventoryFilters");
        var filters = await repository.GetInventoryFiltersAsync(ct);
        logger.LogInformation("GetInventoryFilters returned {Types} types, {Subtypes} subtypes, {Owners} owners",
            filters.Types.Count, filters.Subtypes.Count, filters.Owners.Count);
        return filters;
    }
}
