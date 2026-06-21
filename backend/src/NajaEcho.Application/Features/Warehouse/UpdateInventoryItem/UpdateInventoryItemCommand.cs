namespace NajaEcho.Application.Features.Warehouse.UpdateInventoryItem;

public sealed record UpdateInventoryItemCommand(Guid Id, Guid OwnerUserId, Guid StationId, int Quantity);
