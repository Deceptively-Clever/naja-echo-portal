using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;

namespace NajaEcho.Application.Features.Hangar.GetOrgHangar;

public sealed class GetOrgHangarHandler(IHangarRepository repository)
{
    public Task<PagedResult<OrgShipCard>> HandleAsync(GetOrgHangarQuery query, CancellationToken ct) =>
        repository.GetOrgHangarAsync(
            query.CurrentUserId, query.Search, query.Mine, query.MemberId,
            query.Page, query.PageSize, ct);
}
