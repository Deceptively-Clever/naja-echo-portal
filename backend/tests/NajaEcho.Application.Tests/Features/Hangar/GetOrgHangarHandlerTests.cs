using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class GetOrgHangarHandlerTests
{
    private static readonly Guid UserId1 = Guid.NewGuid();
    private static readonly Guid UserId2 = Guid.NewGuid();
    private static readonly Guid ShipA = Guid.NewGuid();
    private static readonly Guid ShipB = Guid.NewGuid();

    private sealed class ControlledOrgRepo : IHangarRepository
    {
        private readonly List<OrgShipCard> _cards;

        public ControlledOrgRepo(List<OrgShipCard> cards) => _cards = cards;

        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<ShipCard>([], page, pageSize, 0, 0));

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, CancellationToken ct)
        {
            var filtered = _cards.AsEnumerable();

            if (memberId.HasValue)
                filtered = filtered.Where(c => c.Owners.Any(o => o.UserId == memberId.Value));
            else if (mine)
                filtered = filtered.Where(c => c.Owners.Any(o => o.UserId == currentUserId));

            if (search is not null)
                filtered = filtered.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            return Task.FromResult(new PagedResult<OrgShipCard>(list, page, pageSize, list.Count, 1));
        }

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<OwningMember>>([]);

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<CatalogSearchRow>([], page, pageSize, 0, 0));

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private static List<OrgShipCard> BuildCards() =>
    [
        new OrgShipCard(ShipA, "Gladius", "Aegis", null, null, null, 2,
            [new(UserId1, "Alice"), new(UserId2, "Bob")]),
        new OrgShipCard(ShipB, "Hornet", "Anvil", null, null, null, 1,
            [new(UserId2, "Bob")]),
    ];

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllGroupedShips()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, false, null, 1, 25), default);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_OwnerCount_CorrectPerShip()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, false, null, 1, 25), default);

        result.Items.Single(c => c.ShipId == ShipA).OwnerCount.Should().Be(2);
        result.Items.Single(c => c.ShipId == ShipB).OwnerCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_OwnersListPopulated()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, false, null, 1, 25), default);

        var gladius = result.Items.Single(c => c.ShipId == ShipA);
        gladius.Owners.Should().HaveCount(2);
        gladius.Owners.Should().Contain(o => o.DisplayName == "Alice");
        gladius.Owners.Should().Contain(o => o.DisplayName == "Bob");
    }

    [Fact]
    public async Task HandleAsync_MineFlag_ReturnsOnlyCurrentUserShips()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, true, null, 1, 25), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].ShipId.Should().Be(ShipA);
    }

    [Fact]
    public async Task HandleAsync_MemberIdFilter_ReturnsOnlyThatMembersShips()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, false, UserId2, 1, 25), default);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_MemberIdOverridesMine_UsesOnlyMemberId()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        // memberId=UserId2 should override mine=true; should return UserId2's ships (both), not just UserId1's
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, null, true, UserId2, 1, 25), default);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_SearchFilter_ReturnsMatchingShipsOnly()
    {
        var handler = new GetOrgHangarHandler(new ControlledOrgRepo(BuildCards()));
        var result = await handler.HandleAsync(new GetOrgHangarQuery(UserId1, "glad", false, null, 1, 25), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].ShipId.Should().Be(ShipA);
    }
}
