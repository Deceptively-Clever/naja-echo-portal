using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;

namespace NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;

public sealed class ChangeInventoryQuantityHandler(
    IWarehouseInventoryRepository repository,
    ILogger<ChangeInventoryQuantityHandler> logger)
{
    public async Task<InventoryRowDto> HandleAsync(ChangeInventoryQuantityCommand command, CancellationToken ct)
    {
        if (command.Quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be at least 1.");
        }

        logger.LogInformation("ChangeInventoryQuantity rowId={Id} quantity={Quantity}", command.Id, command.Quantity);

        var row = await repository.UpdateQuantityAsync(command.Id, command.Quantity, ct);

        logger.LogInformation("ChangeInventoryQuantity succeeded rowId={Id} newQuantity={Quantity}", row.Id, row.Quantity);
        return row;
    }
}
