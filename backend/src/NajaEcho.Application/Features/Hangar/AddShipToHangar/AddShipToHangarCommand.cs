namespace NajaEcho.Application.Features.Hangar.AddShipToHangar;

public sealed record AddShipToHangarCommand(Guid UserId, Guid ShipId);
