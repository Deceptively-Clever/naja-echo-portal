namespace NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;

public sealed record ChangeInventoryQuantityCommand(Guid Id, int Quantity);
