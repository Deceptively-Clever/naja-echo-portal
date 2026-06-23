namespace NajaEcho.Application.Features.Warehouse.Materials.AddMaterial;

public sealed record AddMaterialCommand(
    Guid CommodityId,
    Guid OwnerUserId,
    string Location,
    decimal Quantity,
    int Quality = 500,
    Guid? LocationId = null,
    string? LocationType = null);
