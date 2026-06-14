using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.GetInventory;

public sealed class GetInventoryHandler(
    IWarehouseInventoryRepository repository,
    ILogger<GetInventoryHandler> logger)
{
    public async Task<IReadOnlyList<InventoryRowDto>> HandleAsync(GetInventoryQuery query, CancellationToken ct)
    {
        logger.LogInformation("GetInventory name={Name} type={Type} subtype={Subtype} owner={Owner} location={Location}",
            query.Name, query.Type, query.Subtype, query.OwnerUserId, query.Location);

        var rows = await repository.GetInventoryAsync(
            query.Name, query.Type, query.Subtype, query.OwnerUserId, query.Location, ct);

        logger.LogInformation("GetInventory returned {Count} rows", rows.Count);
        return rows;
    }
}
