namespace NajaEcho.Application.Features.Commodities.GetCommodities;

public sealed record GetCommoditiesResult(
    IReadOnlyList<CommodityListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
