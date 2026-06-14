namespace NajaEcho.Application.Features.Warehouse.GetInventory;

public sealed record InventoryRowDto(
    Guid Id,
    Guid ItemId,
    string Name,
    string? Type,
    string? Subtype,
    int Quantity,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location);
