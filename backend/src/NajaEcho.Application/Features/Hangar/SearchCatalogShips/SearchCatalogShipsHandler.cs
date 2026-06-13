using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;

namespace NajaEcho.Application.Features.Hangar.SearchCatalogShips;

public sealed class SearchCatalogShipsHandler(IHangarRepository repository)
{
    public Task<PagedResult<CatalogSearchRow>> HandleAsync(SearchCatalogShipsQuery query, CancellationToken ct) =>
        repository.SearchCatalogAsync(query.UserId, query.Search, query.Page, query.PageSize, ct);
}
