using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class GetOwningMembersHandlerTests
{
    private static readonly Guid UserId1 = Guid.NewGuid();
    private static readonly Guid UserId2 = Guid.NewGuid();

    private sealed class FixedMembersRepo : IHangarRepository
    {
        private readonly IReadOnlyList<OwningMember> _members;

        public FixedMembersRepo(IReadOnlyList<OwningMember> members) => _members = members;

        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<ShipCard>([], page, pageSize, 0, 0));

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, string sortBy, CancellationToken ct)
            => Task.FromResult(new PagedResult<OrgShipCard>([], page, pageSize, 0, 0));

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => Task.FromResult(_members);

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<CatalogSearchRow>([], page, pageSize, 0, 0));

        public Task<bool> ExistsAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.CompletedTask;

        public Task ReplaceFromImportAsync(Guid userId, IReadOnlyList<Guid> shipIds, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<Dictionary<string, Guid>> GetShipIdsByNamesAsync(IReadOnlyList<string> names, CancellationToken ct)
            => throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllOwningMembers()
    {
        IReadOnlyList<OwningMember> members = [new(UserId1, "Alice"), new(UserId2, "Bob")];
        var handler = new GetOwningMembersHandler(new FixedMembersRepo(members));

        var result = await handler.HandleAsync(new GetOwningMembersQuery(), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepo_ReturnsEmptyList()
    {
        var handler = new GetOwningMembersHandler(new FixedMembersRepo([]));

        var result = await handler.HandleAsync(new GetOwningMembersQuery(), default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_MembersHaveCorrectDisplayNames()
    {
        IReadOnlyList<OwningMember> members = [new(UserId1, "Alice"), new(UserId2, "Bob")];
        var handler = new GetOwningMembersHandler(new FixedMembersRepo(members));

        var result = await handler.HandleAsync(new GetOwningMembersQuery(), default);

        result.Should().Contain(m => m.DisplayName == "Alice");
        result.Should().Contain(m => m.DisplayName == "Bob");
    }
}
