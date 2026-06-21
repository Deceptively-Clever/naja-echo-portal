namespace NajaEcho.Application.Features.Warehouse.Materials.AddMaterial;

public sealed class OwnerNotFoundException(Guid ownerUserId)
    : Exception($"Owner user {ownerUserId} not found.") { }
