using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.RemoveInventoryItem;

public sealed class RemoveInventoryItemHandler(
    IWarehouseInventoryRepository repository,
    ILogger<RemoveInventoryItemHandler> logger)
{
    public async Task HandleAsync(RemoveInventoryItemCommand command, CancellationToken ct)
    {
        logger.LogInformation("RemoveInventoryItem rowId={Id}", command.Id);
        await repository.RemoveAsync(command.Id, ct);
        logger.LogInformation("RemoveInventoryItem succeeded rowId={Id}", command.Id);
    }
}
