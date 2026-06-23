using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetLocations;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class GetLocationsHandlerTests
{
    private sealed class FakeStationRepo : ISpaceStationRepository
    {
        public string? LastSearch { get; private set; }
        public int LastLimit { get; private set; }
        public List<StationDto> Stations { get; set; } = [];

        public Task<(int, int, int, int, int)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records,
            IReadOnlyDictionary<int, Guid> starSystemMap,
            CancellationToken ct = default) => Task.FromResult((0, 0, 0, 0, 0));

        public Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct)
        {
            LastSearch = search;
            LastLimit = limit;
            IReadOnlyList<StationDto> result = [.. Stations];
            return Task.FromResult(result);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeCityRepo : ICityRepository
    {
        public List<CityDto> Cities { get; set; } = [];

        public Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(
            IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct = default)
            => Task.FromResult((0, 0, 0, 0, 0));

        public Task<IReadOnlyList<CityDto>> SearchActiveCitiesAsync(string? search, int limit, CancellationToken ct)
        {
            IReadOnlyList<CityDto> result = [.. Cities];
            return Task.FromResult(result);
        }
    }

    private static GetLocationsHandler CreateHandler(FakeStationRepo stationRepo, FakeCityRepo? cityRepo = null) =>
        new(stationRepo, cityRepo ?? new FakeCityRepo(), NullLogger<GetLocationsHandler>.Instance);

    [Fact]
    public async Task ReturnsStationsAndCitiesCombined()
    {
        var stationRepo = new FakeStationRepo
        {
            Stations = [new StationDto(Guid.NewGuid(), "ARC-L1 Wide Forest Station")]
        };
        var cityRepo = new FakeCityRepo
        {
            Cities = [new CityDto(Guid.NewGuid(), "Lorville")]
        };

        var result = await CreateHandler(stationRepo, cityRepo).HandleAsync(new GetLocationsQuery(null, 25), default);

        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Type == "Station");
        result.Should().Contain(l => l.Type == "City");
    }

    [Fact]
    public async Task ResultsSortedAlphabetically()
    {
        var stationRepo = new FakeStationRepo
        {
            Stations = [new StationDto(Guid.NewGuid(), "Zeta Station")]
        };
        var cityRepo = new FakeCityRepo
        {
            Cities = [new CityDto(Guid.NewGuid(), "Alpha City")]
        };

        var result = await CreateHandler(stationRepo, cityRepo).HandleAsync(new GetLocationsQuery(null, 25), default);

        result[0].Name.Should().Be("Alpha City");
        result[1].Name.Should().Be("Zeta Station");
    }

    [Fact]
    public async Task PassesSearchTermToRepository()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetLocationsQuery("ARC", 25), default);

        repo.LastSearch.Should().Be("ARC");
    }

    [Fact]
    public async Task ClampsLimitToMax100()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetLocationsQuery(null, 200), default);

        repo.LastLimit.Should().Be(100);
    }

    [Fact]
    public async Task ClampsLimitToMin1()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetLocationsQuery(null, 0), default);

        repo.LastLimit.Should().Be(1);
    }

    [Fact]
    public async Task ReturnsEmptyListWhenNoCatalogExists()
    {
        var repo = new FakeStationRepo { Stations = [] };

        var result = await CreateHandler(repo).HandleAsync(new GetLocationsQuery(null, 25), default);

        result.Should().BeEmpty();
    }
}
