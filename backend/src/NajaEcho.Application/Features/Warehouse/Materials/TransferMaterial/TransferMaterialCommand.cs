namespace NajaEcho.Application.Features.Warehouse.Materials.TransferMaterial;

public sealed record TransferMaterialCommand(Guid RowId, Guid LocationId, string LocationType);
