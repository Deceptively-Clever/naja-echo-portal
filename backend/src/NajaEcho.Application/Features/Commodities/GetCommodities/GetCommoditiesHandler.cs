using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Commodities.GetCommodities;

public sealed class GetCommoditiesHandler(ICommodityRepository repository)
{
    public async Task<GetCommoditiesResult> HandleAsync(GetCommoditiesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(query.Page, query.PageSize, ct);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new GetCommoditiesResult(items, query.Page, query.PageSize, total, totalPages);
    }
}
