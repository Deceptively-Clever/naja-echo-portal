using NajaEcho.Domain.Commodities;

namespace NajaEcho.Application.Features.Commodities.GetCommodities;

public sealed record CommodityListItem(
    Guid Id,
    int UexId,
    string Name,
    string? Code,
    string? Kind,
    CommodityStatus Status);
