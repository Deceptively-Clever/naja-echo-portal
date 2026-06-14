using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Commodities.GetCommodities;

public sealed class GetCommoditiesHandler(ICommodityRepository repository)
{
    public async Task<GetCommoditiesResult> HandleAsync(GetCommoditiesQuery query, CancellationToken ct = default)
    {
        // Normalize paging once, here at the use-case boundary, and report the same values the data reflects.
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await repository.GetPagedAsync(page, pageSize, ct);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new GetCommoditiesResult(items, page, pageSize, total, totalPages);
    }
}
