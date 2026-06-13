using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.AddShipToHangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using NajaEcho.Domain.Ships;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class AddShipToHangarHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ActiveShipId = Guid.NewGuid();
    private static readonly Guid InactiveShipId = Guid.NewGuid();

    private sealed class FakeHangarRepoWithOwnership : IHangarRepository
    {
        private readonly HashSet<Guid> _owned = [];

        public void Own(Guid shipId) => _owned.Add(shipId);

        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<ShipCard>([], page, pageSize, 0, 0));

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<OrgShipCard>([], page, pageSize, 0, 0));

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<OwningMember>>([]);

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
        {
            var rows = new List<CatalogSearchRow>
            {
                new(ActiveShipId, "Gladius", "Aegis", null, null, null, _owned.Contains(ActiveShipId))
            };
            return Task.FromResult(new PagedResult<CatalogSearchRow>(rows, page, pageSize, rows.Count, 1));
        }

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.FromResult(new ShipCard(shipId, "Gladius", "Aegis", null, null, null));

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeShipRepo : IShipRepository
    {
        public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (id == ActiveShipId)
                return Task.FromResult<Ship?>(new Ship { Id = id, Name = "Gladius", Status = ShipStatus.Active, UexId = 1, RawData = System.Text.Json.JsonDocument.Parse("{}"), ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
            if (id == InactiveShipId)
                return Task.FromResult<Ship?>(new Ship { Id = id, Name = "Gone", Status = ShipStatus.SoftDeleted, UexId = 2, RawData = System.Text.Json.JsonDocument.Parse("{}"), ImportedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
            return Task.FromResult<Ship?>(null);
        }

        public Task<Ship?> GetByUexIdAsync(int uexId, CancellationToken ct = default) => Task.FromResult<Ship?>(null);
        public Task<(IReadOnlyList<Ship> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(int Added, int Updated, int Reactivated, int SoftDeleted)> BulkUpsertAsync(IReadOnlyList<Ship> incoming, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<int>> GetAllActiveUexIdsAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task HandleAsync_ActiveShipNotOwned_AddsShip()
    {
        var repo = new FakeHangarRepoWithOwnership();
        var handler = new AddShipToHangarHandler(repo, new FakeShipRepo());

        var result = await handler.HandleAsync(new AddShipToHangarCommand(UserId, ActiveShipId), default);

        result.ShipId.Should().Be(ActiveShipId);
    }

    [Fact]
    public async Task HandleAsync_ShipAlreadyOwned_ThrowsShipAlreadyOwnedException()
    {
        var repo = new FakeHangarRepoWithOwnership();
        repo.Own(ActiveShipId);
        var handler = new AddShipToHangarHandler(repo, new FakeShipRepo());

        await handler.Invoking(h => h.HandleAsync(new AddShipToHangarCommand(UserId, ActiveShipId), default))
            .Should().ThrowAsync<ShipAlreadyOwnedException>();
    }

    [Fact]
    public async Task HandleAsync_InactiveShip_ThrowsShipNotFoundException()
    {
        var repo = new FakeHangarRepoWithOwnership();
        var handler = new AddShipToHangarHandler(repo, new FakeShipRepo());

        await handler.Invoking(h => h.HandleAsync(new AddShipToHangarCommand(UserId, InactiveShipId), default))
            .Should().ThrowAsync<ShipNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_MissingShip_ThrowsShipNotFoundException()
    {
        var repo = new FakeHangarRepoWithOwnership();
        var handler = new AddShipToHangarHandler(repo, new FakeShipRepo());

        await handler.Invoking(h => h.HandleAsync(new AddShipToHangarCommand(UserId, Guid.NewGuid()), default))
            .Should().ThrowAsync<ShipNotFoundException>();
    }
}
