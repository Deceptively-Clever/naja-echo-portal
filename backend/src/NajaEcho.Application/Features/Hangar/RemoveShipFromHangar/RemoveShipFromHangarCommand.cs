namespace NajaEcho.Application.Features.Hangar.RemoveShipFromHangar;

public sealed record RemoveShipFromHangarCommand(Guid UserId, Guid ShipId);
