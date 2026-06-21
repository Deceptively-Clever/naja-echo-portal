namespace NajaEcho.Application.Features.Hangar.AddShipToHangar;

public sealed class ShipNotFoundException(Guid shipId)
    : Exception($"Ship {shipId} not found or is not active.") { }
