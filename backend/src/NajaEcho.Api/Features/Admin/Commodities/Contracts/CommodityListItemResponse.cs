namespace NajaEcho.Api.Features.Admin.Commodities.Contracts;

public sealed record CommodityListItemResponse(
    Guid Id,
    int UexId,
    string Name,
    string? Code,
    string? Kind,
    string Status);
