namespace NajaEcho.Application.Features.Warehouse.Materials.UpdateMaterial;

public sealed record UpdateMaterialCommand(Guid Id, Guid OwnerUserId, Guid StationId, decimal Quantity);
