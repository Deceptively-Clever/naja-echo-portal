namespace NajaEcho.Application.Features.Hangar.AddShipToHangar;

public sealed class ShipAlreadyOwnedException(Guid shipId)
    : Exception($"Ship {shipId} is already in the member's hangar.");
