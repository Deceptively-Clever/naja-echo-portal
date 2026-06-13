namespace NajaEcho.Api.Features.Admin.Ships.Contracts;

public sealed record PagedShipsResponse(
    IReadOnlyList<ShipListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
