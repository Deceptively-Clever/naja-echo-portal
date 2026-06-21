namespace NajaEcho.Application.Features.Warehouse.AddInventoryItem;

public sealed class OwnerNotFoundException(Guid ownerUserId)
    : Exception($"Owner user {ownerUserId} not found.") { }
