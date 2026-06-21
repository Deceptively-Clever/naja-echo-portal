using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;

namespace NajaEcho.Application.Features.Warehouse.TransferInventoryItem;

public sealed class TransferInventoryItemHandler(
    IWarehouseInventoryRepository repo,
    ISpaceStationRepository stationRepo,
    ILogger<TransferInventoryItemHandler> logger)
{
    public async Task HandleAsync(TransferInventoryItemCommand cmd, CancellationToken ct)
    {
        var stationExists = await stationRepo.ExistsAsync(cmd.StationId, ct);
        if (!stationExists)
        {
            throw new InvalidOperationException($"Station with id {cmd.StationId} not found.");
        }

        var rowExists = await repo.ExistsAsync(cmd.RowId, ct);
        if (!rowExists)
        {
            throw new InventoryRowNotFoundException(cmd.RowId);
        }

        await repo.UpdateStationAsync(cmd.RowId, cmd.StationId, ct);

        logger.LogInformation("TransferInventoryItem rowId={RowId} stationId={StationId}", cmd.RowId, cmd.StationId);
    }
}
