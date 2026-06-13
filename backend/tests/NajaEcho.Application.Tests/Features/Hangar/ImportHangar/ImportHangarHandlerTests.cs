using FluentAssertions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.ImportHangar;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Hangar.ImportHangar;

public sealed class ImportHangarHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly Guid GladiusId = Guid.NewGuid();
    private static readonly Guid AvengerTitanId = Guid.NewGuid();
    private static readonly Guid F8CLightningId = Guid.NewGuid();

    private sealed class FakeHangarRepo : IHangarRepository
    {
        public Dictionary<string, Guid> ShipCatalog { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Guid> ReplacedWith { get; private set; } = [];
        public bool ReplaceCalled { get; private set; }

        public Task<Dictionary<string, Guid>> GetShipIdsByNamesAsync(
            IReadOnlyList<string> names, CancellationToken ct)
        {
            var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
                if (ShipCatalog.TryGetValue(name, out var id))
                    result[name] = id;
            return Task.FromResult(result);
        }

        public Task ReplaceFromImportAsync(Guid userId, IReadOnlyList<Guid> shipIds, CancellationToken ct)
        {
            ReplaceCalled = true;
            ReplacedWith = [.. shipIds];
            return Task.CompletedTask;
        }

        // Remaining interface members — not needed for handler tests
        public Task<PagedResult<ShipCard>> GetMyHangarAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
            Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, string sortBy, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
            => throw new NotImplementedException();

        public Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
            Guid userId, string? search, int page, int pageSize, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<bool> ExistsAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static FakeHangarRepo MakeRepo()
    {
        var repo = new FakeHangarRepo();
        repo.ShipCatalog["Gladius"] = GladiusId;
        repo.ShipCatalog["Avenger Titan"] = AvengerTitanId;
        repo.ShipCatalog["F8C Lightning"] = F8CLightningId;
        return repo;
    }

    private static ImportShipRecord Rec(string name, string? shipName = null, string? unidentified = null)
        => new(name, shipName, unidentified);

    [Fact]
    public async Task HandleAsync_AllRecordsMatch_ImportedShipsEqualsMatchCount()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, [Rec("Gladius"), Rec("Avenger Titan")]), default);

        result.ImportedShips.Should().Be(2);
        result.UnmatchedRecords.Should().Be(0);
        result.TotalRecords.Should().Be(2);
        repo.ReplacedWith.Should().BeEquivalentTo([GladiusId, AvengerTitanId]);
    }

    [Fact]
    public async Task HandleAsync_ShipNamePreferredOverName()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        // name is display name, shipName is the canonical catalog name
        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId,
                [Rec("F8C Lightning Executive Edition", shipName: "F8C Lightning")]), default);

        result.ImportedShips.Should().Be(1);
        repo.ReplacedWith.Should().Contain(F8CLightningId);
    }

    [Fact]
    public async Task HandleAsync_FallsBackToNameWhenShipNameAbsent()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, [Rec("Gladius")]), default);

        result.ImportedShips.Should().Be(1);
        repo.ReplacedWith.Should().Contain(GladiusId);
    }

    [Fact]
    public async Task HandleAsync_MatchIsCaseInsensitive()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, [Rec("gladius")]), default);

        result.ImportedShips.Should().Be(1);
        repo.ReplacedWith.Should().Contain(GladiusId);
    }

    [Fact]
    public async Task HandleAsync_DuplicateRecordsSameShip_SingleHangarEntry()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, [Rec("Gladius"), Rec("Gladius")]), default);

        result.ImportedShips.Should().Be(1);
        repo.ReplacedWith.Should().HaveCount(1);
        repo.ReplacedWith.Should().Contain(GladiusId);
    }

    [Fact]
    public async Task HandleAsync_UnidentifiedRecord_IsSkipped()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId,
                [Rec("A.T.L.S.", unidentified: "Please report this ship")]), default);

        result.ImportedShips.Should().Be(0);
        result.UnmatchedRecords.Should().Be(1);
        result.TotalRecords.Should().Be(1);
        repo.ReplacedWith.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_NoMatchingShips_ImportedIsZero()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId,
                [Rec("Unknown Ship Alpha"), Rec("Unknown Ship Beta")]), default);

        result.ImportedShips.Should().Be(0);
        result.UnmatchedRecords.Should().Be(2);
        result.UnmatchedShipNames.Should().BeEquivalentTo(["Unknown Ship Alpha", "Unknown Ship Beta"]);
    }

    [Fact]
    public async Task HandleAsync_MixedMatchAndUnmatched_CountsCorrect()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, [
                Rec("Gladius"),
                Rec("A.T.L.S.", unidentified: "unrecognized"),
                Rec("Nonexistent Ship"),
            ]), default);

        result.TotalRecords.Should().Be(3);
        result.ImportedShips.Should().Be(1);
        result.UnmatchedRecords.Should().Be(2);
        result.UnmatchedShipNames.Should().Contain("Nonexistent Ship");
        result.UnmatchedShipNames.Should().NotContain("Gladius");
    }

    [Fact]
    public async Task HandleAsync_EmptyItems_ClearsHangar()
    {
        var repo = MakeRepo();
        var handler = new ImportHangarHandler(repo);

        var result = await handler.HandleAsync(
            new ImportHangarCommand(UserId, []), default);

        result.ImportedShips.Should().Be(0);
        result.TotalRecords.Should().Be(0);
        repo.ReplaceCalled.Should().BeTrue();
        repo.ReplacedWith.Should().BeEmpty();
    }
}
