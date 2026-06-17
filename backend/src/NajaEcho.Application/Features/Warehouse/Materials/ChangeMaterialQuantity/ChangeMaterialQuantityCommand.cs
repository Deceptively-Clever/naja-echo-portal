namespace NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;

public sealed record ChangeMaterialQuantityCommand(Guid Id, decimal Quantity);
