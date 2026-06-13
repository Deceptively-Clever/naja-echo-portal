using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class SearchCatalogShipsHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OwnedShipId = Guid.NewGuid();
    private static readonly Guid UnownedShipId = Guid.NewGuid();

    private sealed class FakeCatalogRepo : IHangarRepository
    {
        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<ShipCard>([], page, pageSize, 0, 0));

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, string sortBy, CancellationToken ct)
            => Task.FromResult(new PagedResult<OrgShipCard>([], page, pageSize, 0, 0));

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<OwningMember>>([]);

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
        {
            var all = new List<CatalogSearchRow>
            {
                new(OwnedShipId, "Gladius", "Aegis", null, null, null, true),
                new(UnownedShipId, "Hornet", "Anvil", null, null, null, false),
            };

            var filtered = search is null
                ? all
                : all.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            return Task.FromResult(new PagedResult<CatalogSearchRow>(filtered, page, pageSize, filtered.Count, 1));
        }

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
    public async Task HandleAsync_ReturnsAllCatalogResults_WhenSearchIsNull()
    {
        var handler = new SearchCatalogShipsHandler(new FakeCatalogRepo());
        var result = await handler.HandleAsync(new SearchCatalogShipsQuery(UserId, null, 1, 25), default);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_AlreadyOwnedFlag_SetCorrectly()
    {
        var handler = new SearchCatalogShipsHandler(new FakeCatalogRepo());
        var result = await handler.HandleAsync(new SearchCatalogShipsQuery(UserId, null, 1, 25), default);

        result.Items.Single(r => r.ShipId == OwnedShipId).AlreadyOwned.Should().BeTrue();
        result.Items.Single(r => r.ShipId == UnownedShipId).AlreadyOwned.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_SearchFilters_ReturnsMatchingShipsOnly()
    {
        var handler = new SearchCatalogShipsHandler(new FakeCatalogRepo());
        var result = await handler.HandleAsync(new SearchCatalogShipsQuery(UserId, "glad", 1, 25), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].ShipId.Should().Be(OwnedShipId);
    }
}
