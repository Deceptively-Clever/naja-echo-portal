namespace NajaEcho.Application.Features.Warehouse.GetInventory;

public sealed record GetInventoryQuery(
    string? Name,
    string? Type,
    string? Subtype,
    Guid? OwnerUserId,
    string? Location);
