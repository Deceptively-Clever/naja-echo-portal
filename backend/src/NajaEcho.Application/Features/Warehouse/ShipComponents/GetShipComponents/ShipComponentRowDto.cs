namespace NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;

public sealed record ShipComponentRowDto(
    Guid Id,
    Guid ItemId,
    string Name,
    string? Type,
    string? Class,
    int? Size,
    string? Grade,
    int Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location);
