using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;

namespace NajaEcho.Application.Features.Warehouse.UpdateInventoryItem;

public sealed class UpdateInventoryItemHandler(
    IWarehouseInventoryRepository repository,
    ILogger<UpdateInventoryItemHandler> logger)
{
    public async Task<InventoryRowDto> HandleAsync(UpdateInventoryItemCommand command, CancellationToken ct)
    {
        if (command.Quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be at least 1.");
        }

        logger.LogInformation(
            "UpdateInventoryItem rowId={Id} ownerUserId={OwnerUserId} locationId={LocationId} locationType={LocationType} quantity={Quantity}",
            command.Id, command.OwnerUserId, command.LocationId, command.LocationType, command.Quantity);

        var row = await repository.UpdateItemAsync(command.Id, command.OwnerUserId, command.LocationId, command.LocationType, command.Quantity, ct);

        logger.LogInformation("UpdateInventoryItem succeeded rowId={Id} newQuantity={Quantity} newOwner={OwnerUserId} newLocationId={LocationId}",
            row.Id, row.Quantity, row.OwnerUserId, row.LocationId);

        return row;
    }
}
