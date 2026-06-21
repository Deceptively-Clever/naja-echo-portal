using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.GetStations;
using Xunit;

namespace NajaEcho.Application.Tests.Features.Warehouse;

public sealed class GetStationsHandlerTests
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
            IReadOnlyList<StationDto> result = [..Stations];
            return Task.FromResult(result);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
    }

    private static GetStationsHandler CreateHandler(FakeStationRepo repo) =>
        new(repo, NullLogger<GetStationsHandler>.Instance);

    [Fact]
    public async Task ReturnsListFromRepository()
    {
        var repo = new FakeStationRepo
        {
            Stations =
            [
                new StationDto(Guid.NewGuid(), "ARC-L1 Wide Forest Station"),
                new StationDto(Guid.NewGuid(), "CRU-L1 Ambitious Dream Station"),
            ]
        };

        var result = await CreateHandler(repo).HandleAsync(new GetStationsQuery(null, 25), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task PassesSearchTermToRepository()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetStationsQuery("ARC", 25), default);

        repo.LastSearch.Should().Be("ARC");
    }

    [Fact]
    public async Task ClampsLimitToMax100()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetStationsQuery(null, 200), default);

        repo.LastLimit.Should().Be(100);
    }

    [Fact]
    public async Task ClampsLimitToMin1()
    {
        var repo = new FakeStationRepo { Stations = [] };

        await CreateHandler(repo).HandleAsync(new GetStationsQuery(null, 0), default);

        repo.LastLimit.Should().Be(1);
    }

    [Fact]
    public async Task ReturnsEmptyListWhenNoCatalogExists()
    {
        var repo = new FakeStationRepo { Stations = [] };

        var result = await CreateHandler(repo).HandleAsync(new GetStationsQuery(null, 25), default);

        result.Should().BeEmpty();
    }
}
