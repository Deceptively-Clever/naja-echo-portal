using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.RemoveShipFromHangar;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar;

public sealed class RemoveShipFromHangarHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ShipId = Guid.NewGuid();

    private sealed class TrackingHangarRepo : IHangarRepository
    {
        public List<(Guid UserId, Guid ShipId)> RemovedEntries { get; } = [];

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
            => Task.FromResult(new PagedResult<CatalogSearchRow>([], page, pageSize, 0, 0));

        public Task<bool> ExistsAsync(Guid userId, Guid shipId, CancellationToken ct)
            => Task.FromResult(false);

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
        {
            RemovedEntries.Add((userId, shipId));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task HandleAsync_CallsRepoRemoveWithCorrectIds()
    {
        var repo = new TrackingHangarRepo();
        var handler = new RemoveShipFromHangarHandler(repo);

        await handler.HandleAsync(new RemoveShipFromHangarCommand(UserId, ShipId), default);

        repo.RemovedEntries.Should().ContainSingle()
            .Which.Should().Be((UserId, ShipId));
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_DoesNotThrowForNonExistentEntry()
    {
        var repo = new TrackingHangarRepo();
        var handler = new RemoveShipFromHangarHandler(repo);

        var act = () => handler.HandleAsync(new RemoveShipFromHangarCommand(UserId, Guid.NewGuid()), default);

        await act.Should().NotThrowAsync();
    }
}
