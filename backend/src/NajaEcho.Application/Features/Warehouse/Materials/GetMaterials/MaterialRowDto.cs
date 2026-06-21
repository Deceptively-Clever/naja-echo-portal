namespace NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

public sealed record MaterialRowDto(
    Guid Id,
    Guid CommodityId,
    string MaterialName,
    string? MaterialCode,
    decimal Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location,
    Guid? StationId = null);
