namespace NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;

public sealed class InventoryRowNotFoundException(Guid id)
    : Exception($"Inventory row {id} not found.");
