using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;

namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed class AddInventoryItemHandler(
    IWarehouseInventoryRepository repository,
    IItemRepository itemRepository,
    IUserRepository userRepository,
    ILogger<AddInventoryItemHandler> logger)
{
    public async Task<(InventoryRowDto Row, bool IsNew)> HandleAsync(AddInventoryItemCommand command, CancellationToken ct)
    {
        var location = command.Location.Trim();

        if (string.IsNullOrEmpty(location))
            throw new ArgumentException("Location must not be empty.", nameof(command));

        if (command.Quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be at least 1.");

        var item = await itemRepository.GetByIdAsync(command.ItemId, ct);
        if (item is null || item.Status != NajaEcho.Domain.Items.ItemStatus.Active)
            throw new ItemNotFoundException(command.ItemId);

        var ownerExists = await userRepository.ExistsAsync(command.OwnerUserId, ct);
        if (!ownerExists)
            throw new OwnerNotFoundException(command.OwnerUserId);

        logger.LogInformation("AddInventoryItem itemId={ItemId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity}",
            command.ItemId, command.OwnerUserId, location, command.Quantity);

        var (row, isNew) = await repository.AddOrIncrementAsync(command.ItemId, command.OwnerUserId, location, command.Quantity, ct);

        logger.LogInformation("AddInventoryItem {Action} rowId={RowId} quantity={Quantity}",
            isNew ? "created" : "incremented", row.Id, row.Quantity);

        return (row, isNew);
    }
}
