namespace NajaEcho.Application.Features.Warehouse.Materials.AddMaterial;

public sealed class CommodityNotFoundException(Guid commodityId)
    : Exception($"Commodity {commodityId} not found or is not active.") { }
