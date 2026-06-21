using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Locations.ImportLocations;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Locations;

public sealed class ImportLocationsHandlerTests
{
    private sealed class FakeUexLocationClient : IUexLocationClient
    {
        public IReadOnlyList<JsonDocument> StarSystems { get; set; } = [];
        public IReadOnlyList<JsonDocument> SpaceStations { get; set; } = [];
        public bool StarSystemsThrows { get; set; }
        public bool SpaceStationsThrows { get; set; }

        public Task<IReadOnlyList<JsonDocument>> FetchAllStarSystemsAsync(CancellationToken ct)
        {
            if (StarSystemsThrows) throw new HttpRequestException("Source unreachable");
            return Task.FromResult(StarSystems);
        }

        public Task<IReadOnlyList<JsonDocument>> FetchAllSpaceStationsAsync(CancellationToken ct)
        {
            if (SpaceStationsThrows) throw new HttpRequestException("Source unreachable");
            return Task.FromResult(SpaceStations);
        }
    }

    private sealed class FakeStarSystemRepository : IStarSystemRepository
    {
        public int BulkUpsertCallCount { get; private set; }
        public (int added, int updated, int reactivated, int softDeleted) Counts { get; set; } = (0, 0, 0, 0);
        public IReadOnlyDictionary<int, Guid> ActiveMap { get; set; } = new Dictionary<int, Guid>();

        public Task<(int added, int updated, int reactivated, int softDeleted)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records, CancellationToken ct)
        {
            BulkUpsertCallCount++;
            return Task.FromResult(Counts);
        }

        public Task<IReadOnlyDictionary<int, Guid>> GetActiveUexIdToIdMapAsync(CancellationToken ct)
            => Task.FromResult(ActiveMap);
    }

    private sealed class FakeSpaceStationRepository : ISpaceStationRepository
    {
        public int BulkUpsertCallCount { get; private set; }
        public (int added, int updated, int reactivated, int softDeleted, int skipped) Counts { get; set; } = (0, 0, 0, 0, 0);

        public Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct)
        {
            BulkUpsertCallCount++;
            return Task.FromResult(Counts);
        }

        public Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StationDto>>([]);

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(true);
    }

    private sealed class FakeCoordinator : IImportCoordinator
    {
        public bool IsHeld { get; private set; }
        public bool TryAcquire() { if (IsHeld) return false; IsHeld = true; return true; }
        public void Release() => IsHeld = false;
    }

    private static JsonDocument MakeDoc(string json) => JsonDocument.Parse(json);

    private static ImportLocationsHandler CreateHandler(
        FakeUexLocationClient client,
        FakeStarSystemRepository starRepo,
        FakeSpaceStationRepository stationRepo,
        FakeCoordinator? coordinator = null) =>
        new(client, starRepo, stationRepo, coordinator ?? new FakeCoordinator(), NullLogger<ImportLocationsHandler>.Instance);

    [Fact]
    public async Task HappyPath_ReturnsSeparateCountsForBothEntities()
    {
        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}"), MakeDoc("{}"), MakeDoc("{}")],
            SpaceStations = [MakeDoc("{}"), MakeDoc("{}"), MakeDoc("{}"), MakeDoc("{}"), MakeDoc("{}")],
        };
        var starRepo = new FakeStarSystemRepository { Counts = (3, 0, 0, 0) };
        var stationRepo = new FakeSpaceStationRepository { Counts = (5, 0, 0, 0, 0) };

        var result = await CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        result.StarSystems.Added.Should().Be(3);
        result.SpaceStations.Added.Should().Be(5);
    }

    [Fact]
    public async Task EmptyStarSystemsFeed_ThrowsEmptySourceException_NoWritesOccur()
    {
        var client = new FakeUexLocationClient
        {
            StarSystems = [],
            SpaceStations = [MakeDoc("{}")],
        };
        var starRepo = new FakeStarSystemRepository();
        var stationRepo = new FakeSpaceStationRepository();

        var act = () => CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        await act.Should().ThrowAsync<EmptySourceException>();
        starRepo.BulkUpsertCallCount.Should().Be(0);
        stationRepo.BulkUpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task EmptyStationsFeed_ThrowsEmptySourceException_NoWritesOccur()
    {
        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}")],
            SpaceStations = [],
        };
        var starRepo = new FakeStarSystemRepository();
        var stationRepo = new FakeSpaceStationRepository();

        var act = () => CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        await act.Should().ThrowAsync<EmptySourceException>();
        stationRepo.BulkUpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UnreachableSource_HttpRequestException_Propagates()
    {
        var client = new FakeUexLocationClient { StarSystemsThrows = true };
        var starRepo = new FakeStarSystemRepository();
        var stationRepo = new FakeSpaceStationRepository();

        var act = () => CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        await act.Should().ThrowAsync<HttpRequestException>();
        starRepo.BulkUpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task StationWithUnknownParentStarSystem_IsSkippedAndCounted()
    {
        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}")],
            SpaceStations = [MakeDoc("{}")],
        };
        var starRepo = new FakeStarSystemRepository { Counts = (1, 0, 0, 0) };
        var stationRepo = new FakeSpaceStationRepository { Counts = (0, 0, 0, 0, 1) };

        var result = await CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        result.SpaceStations.Skipped.Should().Be(1);
        result.SpaceStations.Added.Should().Be(0);
    }

    [Fact]
    public async Task SoftDeletesAbsentRecords_ReturnsCount()
    {
        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}")],
            SpaceStations = [MakeDoc("{}")],
        };
        var starRepo = new FakeStarSystemRepository { Counts = (0, 0, 0, 1) };
        var stationRepo = new FakeSpaceStationRepository { Counts = (0, 0, 0, 0, 0) };

        var result = await CreateHandler(client, starRepo, stationRepo).HandleAsync(new ImportLocationsCommand(), default);

        result.StarSystems.SoftDeleted.Should().Be(1);
    }

    [Fact]
    public async Task ImportAlreadyInProgress_ThrowsImportAlreadyInProgressException()
    {
        var coordinator = new FakeCoordinator();
        coordinator.TryAcquire();

        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}")],
            SpaceStations = [MakeDoc("{}")],
        };

        var act = () => CreateHandler(client, new FakeStarSystemRepository(), new FakeSpaceStationRepository(), coordinator)
            .HandleAsync(new ImportLocationsCommand(), default);

        await act.Should().ThrowAsync<ImportAlreadyInProgressException>();
    }

    [Fact]
    public async Task ReleasesLockAfterSuccess()
    {
        var coordinator = new FakeCoordinator();
        var client = new FakeUexLocationClient
        {
            StarSystems = [MakeDoc("{}")],
            SpaceStations = [MakeDoc("{}")],
        };
        var starRepo = new FakeStarSystemRepository { Counts = (1, 0, 0, 0) };
        var stationRepo = new FakeSpaceStationRepository { Counts = (1, 0, 0, 0, 0) };

        await CreateHandler(client, starRepo, stationRepo, coordinator).HandleAsync(new ImportLocationsCommand(), default);

        coordinator.IsHeld.Should().BeFalse();
    }

    [Fact]
    public async Task ReleasesLockAfterFailure()
    {
        var coordinator = new FakeCoordinator();
        var client = new FakeUexLocationClient { StarSystems = [] };

        var act = () => CreateHandler(client, new FakeStarSystemRepository(), new FakeSpaceStationRepository(), coordinator)
            .HandleAsync(new ImportLocationsCommand(), default);

        await act.Should().ThrowAsync<EmptySourceException>();
        coordinator.IsHeld.Should().BeFalse();
    }
}
