using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;

namespace NajaEcho.Application.Features.Hangar.GetMyHangar;

public sealed class GetMyHangarHandler(IHangarRepository repository)
{
    public Task<PagedResult<ShipCard>> HandleAsync(GetMyHangarQuery query, CancellationToken ct) =>
        repository.GetMyHangarAsync(query.UserId, query.Search, query.Page, query.PageSize, ct);
}
