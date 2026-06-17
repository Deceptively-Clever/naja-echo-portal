namespace NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;

public sealed class MaterialRowNotFoundException(Guid id)
    : Exception($"Material row {id} not found.");
