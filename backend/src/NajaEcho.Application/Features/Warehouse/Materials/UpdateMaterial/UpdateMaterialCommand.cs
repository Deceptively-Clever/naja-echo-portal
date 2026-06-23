namespace NajaEcho.Application.Features.Warehouse.Materials.UpdateMaterial;

public sealed record UpdateMaterialCommand(Guid Id, Guid OwnerUserId, Guid LocationId, string LocationType, decimal Quantity);
