using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Commodities.GetCommodities;

public sealed class GetCommoditiesHandler(ICommodityRepository repository)
{
    public async Task<GetCommoditiesResult> HandleAsync(GetCommoditiesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(query.Page, query.PageSize, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.PageSize);

        var listItems = items
            .Select(c => new CommodityListItem(c.Id, c.UexId, c.Name, c.Code, c.Kind, c.Status))
            .ToList();

        return new GetCommoditiesResult(listItems, query.Page, query.PageSize, total, totalPages);
    }
}
