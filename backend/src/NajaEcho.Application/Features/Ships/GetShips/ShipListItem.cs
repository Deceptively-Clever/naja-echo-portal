using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Features.Ships.GetShips;

public sealed record ShipListItem(Guid Id, string Name, string? CompanyName, ShipStatus Status);
