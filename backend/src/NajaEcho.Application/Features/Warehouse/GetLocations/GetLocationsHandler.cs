using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.GetLocations;

public sealed class GetLocationsHandler(
    ISpaceStationRepository stationRepo,
    ICityRepository cityRepo,
    ILogger<GetLocationsHandler> logger)
{
    public async Task<IReadOnlyList<LocationDto>> HandleAsync(GetLocationsQuery query, CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);

        var stationResults = await stationRepo.SearchActiveStationsAsync(query.Search, limit, ct);
        var cityResults = await cityRepo.SearchActiveCitiesAsync(query.Search, limit, ct);

        var stations = stationResults.Select(s => new LocationDto(s.Id, s.Name, "Station"));
        var cities = cityResults.Select(c => new LocationDto(c.Id, c.Name, "City"));

        var combined = stations.Concat(cities)
            .OrderBy(l => l.Name)
            .Take(limit)
            .ToList();

        logger.LogInformation(
            "GetLocations search={Search} limit={Limit} returned {Count} results",
            query.Search, limit, combined.Count);

        return combined;
    }
}
