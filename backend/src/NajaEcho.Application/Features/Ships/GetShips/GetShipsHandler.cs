using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Ships.GetShips;

public sealed class GetShipsHandler(IShipRepository repository)
{
    public async Task<GetShipsResult> HandleAsync(GetShipsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(query.Page, query.PageSize, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.PageSize);

        var listItems = items
            .Select(s => new ShipListItem(s.Id, s.Name, s.CompanyName, s.Status))
            .ToList();

        return new GetShipsResult(listItems, query.Page, query.PageSize, total, totalPages);
    }
}
