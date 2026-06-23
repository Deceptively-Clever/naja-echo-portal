namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed record AddInventoryItemCommand(
    Guid ItemId,
    Guid OwnerUserId,
    string Location,
    int Quantity,
    int Quality = 500,
    Guid? LocationId = null,
    string? LocationType = null);
