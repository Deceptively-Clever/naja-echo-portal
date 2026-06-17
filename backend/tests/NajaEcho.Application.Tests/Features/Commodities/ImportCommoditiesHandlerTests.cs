using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Commodities.GetCommodities;
using NajaEcho.Application.Features.Commodities.ImportCommodities;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.Commodities;

namespace NajaEcho.Application.Tests.Features.Commodities;

public sealed class ImportCommoditiesHandlerTests
{
    private sealed class FakeCommodityRepository : ICommodityRepository
    {
        public List<Commodity> Upserted { get; } = [];

        public Task<(int Inserted, int Updated, int Unchanged, int Restored, int SoftDeleted)> BulkUpsertAsync(
            IReadOnlyList<Commodity> incoming, CancellationToken ct)
        {
            Upserted.AddRange(incoming);
            return Task.FromResult((incoming.Count, 0, 0, 0, 0));
        }

        public Task<(IReadOnlyList<CommodityListItem> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(((IReadOnlyList<CommodityListItem>)[], 0));

        public Task<Commodity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Commodity?>(null);
    }

    private sealed class FakeCommodityClient : IUexCommodityClient
    {
        public IReadOnlyList<JsonDocument> Records { get; set; } = [];
        public bool Throws { get; set; }

        public Task<IReadOnlyList<JsonDocument>> FetchAllCommoditiesAsync(CancellationToken ct)
        {
            if (Throws) throw new HttpRequestException("Feed unavailable");
            return Task.FromResult(Records);
        }
    }

    private sealed class FakeCoordinator : IImportCoordinator
    {
        public bool IsHeld { get; private set; }
        public bool TryAcquire() { if (IsHeld) return false; IsHeld = true; return true; }
        public void Release() => IsHeld = false;
    }

    private static JsonDocument MakeRecord(int id, string name, string? uuid = null,
        string? idsStarSystems = null, int dateAdded = 0, bool isAvailable = true, bool isFuel = false) =>
        JsonDocument.Parse($$"""
        {
            "id": {{id}},
            "name": "{{name}}",
            "uuid": {{(uuid is null ? "null" : $"\"{uuid}\"")}},
            "code": "CODE{{id}}",
            "slug": "slug-{{id}}",
            "kind": "commodity",
            "weight_scu": 1,
            "id_parent": null,
            "id_item": null,
            "ids_star_systems": {{(idsStarSystems is null ? "null" : $"\"{idsStarSystems}\"")}},
            "ids_planets": null,
            "ids_moons": null,
            "ids_poi": null,
            "ids_orbits": null,
            "wiki": null,
            "is_available": {{(isAvailable ? 1 : 0)}},
            "is_available_live": 0,
            "is_visible": 1,
            "is_extractable": 0,
            "is_mineral": 0,
            "is_raw": 0,
            "is_pure": 0,
            "is_refined": 0,
            "is_refinable": 0,
            "is_harvestable": 0,
            "is_buyable": 1,
            "is_sellable": 1,
            "is_temporary": 0,
            "is_illegal": 0,
            "is_volatile_qt": 0,
            "is_volatile_time": 0,
            "is_inert": 0,
            "is_explosive": 0,
            "is_buggy": 0,
            "is_fuel": {{(isFuel ? 1 : 0)}},
            "date_added": {{dateAdded}},
            "date_modified": 0
        }
        """);

    private ImportCommoditiesHandler CreateHandler(
        FakeCommodityRepository repo, FakeCommodityClient client, FakeCoordinator coordinator) =>
        new(repo, client, coordinator, NullLogger<ImportCommoditiesHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ValidFeed_ReturnsInsertedCount()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient
        {
            Records = [MakeRecord(1, "Agricium"), MakeRecord(2, "Laranite")]
        };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportCommoditiesCommand());

        result.Fetched.Should().Be(2);
        result.Inserted.Should().Be(2);
        result.Skipped.Should().Be(0);
        result.Warning.Should().BeNull();
        repo.Upserted.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_EmptyFeed_ReturnsWarningAndZeroCounts()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient { Records = [] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportCommoditiesCommand());

        result.Fetched.Should().Be(0);
        result.Inserted.Should().Be(0);
        result.Warning.Should().NotBeNullOrEmpty();
        repo.Upserted.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_RecordMissingId_SkipsRecord()
    {
        var repo = new FakeCommodityRepository();
        var missingId = JsonDocument.Parse("""{"id": 0, "name": "No Id Commodity", "code": "NIC"}""");
        var valid = MakeRecord(5, "Agricium");
        var client = new FakeCommodityClient { Records = [missingId, valid] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportCommoditiesCommand());

        result.Fetched.Should().Be(2);
        result.Skipped.Should().Be(1);
        result.Inserted.Should().Be(1);
        repo.Upserted.Should().HaveCount(1);
        repo.Upserted[0].Name.Should().Be("Agricium");
    }

    [Fact]
    public async Task HandleAsync_RecordMissingName_SkipsRecord()
    {
        var repo = new FakeCommodityRepository();
        var missingName = JsonDocument.Parse("""{"id": 42, "code": "X"}""");
        var valid = MakeRecord(5, "Laranite");
        var client = new FakeCommodityClient { Records = [missingName, valid] };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        var result = await handler.HandleAsync(new ImportCommoditiesCommand());

        result.Skipped.Should().Be(1);
        result.Inserted.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_NormalizesBoolFlags_FromIntegerValues()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient
        {
            Records = [MakeRecord(1, "Fuel", isFuel: true, isAvailable: false)]
        };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        await handler.HandleAsync(new ImportCommoditiesCommand());

        var commodity = repo.Upserted[0];
        commodity.IsFuel.Should().BeTrue();
        commodity.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ParsesCommaSeparatedIdList()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient
        {
            Records = [MakeRecord(1, "Ore", idsStarSystems: "1,2,3")]
        };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        await handler.HandleAsync(new ImportCommoditiesCommand());

        repo.Upserted[0].IdsStarSystems.Should().Equal(1, 2, 3);
        repo.Upserted[0].IdsStarSystemsRaw.Should().Be("1,2,3");
    }

    [Fact]
    public async Task HandleAsync_ParsesUnixTimestampToDualStorage()
    {
        var repo = new FakeCommodityRepository();
        const int unixTs = 1700000000;
        var client = new FakeCommodityClient
        {
            Records = [MakeRecord(1, "Ore", dateAdded: unixTs)]
        };
        var handler = CreateHandler(repo, client, new FakeCoordinator());

        await handler.HandleAsync(new ImportCommoditiesCommand());

        var commodity = repo.Upserted[0];
        commodity.SourceDateAdded.Should().Be(unixTs);
        commodity.SourceDateAddedUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(unixTs));
    }

    [Fact]
    public async Task HandleAsync_FeedFetchFails_ThrowsHttpRequestException()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient { Throws = true };
        var coordinator = new FakeCoordinator();
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new ImportCommoditiesCommand()))
            .Should().ThrowAsync<HttpRequestException>();

        coordinator.IsHeld.Should().BeFalse("lock must be released even on failure");
    }

    [Fact]
    public async Task HandleAsync_ConcurrentImport_ThrowsImportAlreadyInProgressException()
    {
        var repo = new FakeCommodityRepository();
        var client = new FakeCommodityClient { Records = [MakeRecord(1, "Ore")] };
        var coordinator = new FakeCoordinator();
        coordinator.TryAcquire(); // simulate lock held by another import
        var handler = CreateHandler(repo, client, coordinator);

        await handler.Invoking(h => h.HandleAsync(new ImportCommoditiesCommand()))
            .Should().ThrowAsync<ImportAlreadyInProgressException>();
    }
}
