using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;

namespace NajaEcho.Application.Abstractions;

public interface IHangarRepository
{
    Task<PagedResult<ShipCard>> GetMyHangarAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken ct);

    Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
        Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct);

    Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken ct);

    Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct);

    Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct);
}
