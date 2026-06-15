using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetInventory;

namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed class AddInventoryItemHandler(
    IWarehouseInventoryRepository repository,
    IItemRepository itemRepository,
    IUserRepository userRepository,
    IShipComponentRepository shipComponentRepository,
    IUexItemAttributeClient uexAttributeClient,
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

        await TryFetchAndCacheAttributesAsync(command.ItemId, item.UexId, ct);

        var (row, isNew) = await repository.AddOrIncrementAsync(command.ItemId, command.OwnerUserId, location, command.Quantity, ct);

        logger.LogInformation("AddInventoryItem {Action} rowId={RowId} quantity={Quantity}",
            isNew ? "created" : "incremented", row.Id, row.Quantity);

        return (row, isNew);
    }

    private async Task TryFetchAndCacheAttributesAsync(Guid itemId, int uexItemId, CancellationToken ct)
    {
        if (uexItemId <= 0) return;

        var hasCached = await shipComponentRepository.HasCachedAttributesAsync(itemId, ct);
        if (hasCached)
        {
            logger.LogInformation("AddInventoryItem attribute cache hit for itemId={ItemId}", itemId);
            return;
        }

        logger.LogInformation("AddInventoryItem lazy-fetching attributes uexItemId={UexId}", uexItemId);
        try
        {
            var rawDocs = await uexAttributeClient.FetchItemAttributesAsync(uexItemId, ct);
            var fetchedAt = DateTimeOffset.UtcNow;
            var attrs = UexAttributeParser.Parse(itemId, uexItemId, rawDocs, fetchedAt);
            if (attrs.Count > 0)
            {
                await shipComponentRepository.SaveItemAttributesAsync(attrs, ct);
                logger.LogInformation("AddInventoryItem cached {Count} attributes for itemId={ItemId}", attrs.Count, itemId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AddInventoryItem attribute fetch failed for itemId={ItemId} uexId={UexId} — continuing without attributes",
                itemId, uexItemId);
        }
    }
}
