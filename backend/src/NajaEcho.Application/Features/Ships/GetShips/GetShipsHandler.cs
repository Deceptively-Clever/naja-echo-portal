using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Ships.GetShips;

public sealed class GetShipsHandler(IShipRepository repository)
{
    public async Task<GetShipsResult> HandleAsync(GetShipsQuery query, CancellationToken ct = default)
    {
        // Normalize paging once, here at the use-case boundary, and report the same values the data reflects.
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await repository.GetPagedAsync(page, pageSize, ct);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var listItems = items
            .Select(s => new ShipListItem(s.Id, s.Name, s.CompanyName, s.Status))
            .ToList();

        return new GetShipsResult(listItems, page, pageSize, total, totalPages);
    }
}
