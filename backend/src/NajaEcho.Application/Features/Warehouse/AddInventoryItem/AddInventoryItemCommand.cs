namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed record AddInventoryItemCommand(
    Guid ItemId,
    Guid OwnerUserId,
    string Location,
    int Quantity);
