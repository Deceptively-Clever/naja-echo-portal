namespace NajaEcho.Application.Features.Warehouse.GetInventory;

public sealed record InventoryRowDto(
    Guid Id,
    Guid ItemId,
    string Name,
    string? Type,
    string? Subtype,
    int Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location,
    Guid? LocationId = null,
    string? LocationType = null);
