namespace NajaEcho.Api.Features.Admin.Commodities.Contracts;

public sealed record PagedCommoditiesResponse(
    IReadOnlyList<CommodityListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
