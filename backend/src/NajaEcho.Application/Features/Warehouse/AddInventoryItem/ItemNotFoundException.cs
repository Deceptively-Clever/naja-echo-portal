namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed class ItemNotFoundException(Guid itemId)
    : Exception($"Catalog item {itemId} not found or is not active.");
