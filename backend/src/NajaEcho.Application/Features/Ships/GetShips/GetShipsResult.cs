namespace NajaEcho.Application.Features.Ships.GetShips;

public sealed record GetShipsResult(
    IReadOnlyList<ShipListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
