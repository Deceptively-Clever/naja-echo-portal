using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;

namespace NajaEcho.Application.Features.Warehouse.TransferInventoryItem;

public sealed class TransferInventoryItemHandler(
    IWarehouseInventoryRepository repo,
    ILogger<TransferInventoryItemHandler> logger)
{
    public async Task HandleAsync(TransferInventoryItemCommand cmd, CancellationToken ct)
    {
        var rowExists = await repo.ExistsAsync(cmd.RowId, ct);
        if (!rowExists)
        {
            throw new InventoryRowNotFoundException(cmd.RowId);
        }

        await repo.UpdateLocationAsync(cmd.RowId, cmd.LocationId, cmd.LocationType, ct);

        logger.LogInformation("TransferInventoryItem rowId={RowId} locationId={LocationId} locationType={LocationType}",
            cmd.RowId, cmd.LocationId, cmd.LocationType);
    }
}
