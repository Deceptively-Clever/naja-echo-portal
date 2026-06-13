using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Features.Ships.GetShipById;

public sealed record ShipDetail(Guid Id, ShipStatus Status, IReadOnlyDictionary<string, object?> Fields);
