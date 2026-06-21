using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.GetStations;

public sealed class GetStationsHandler(ISpaceStationRepository repo, ILogger<GetStationsHandler> logger)
{
    public async Task<IReadOnlyList<StationDto>> HandleAsync(GetStationsQuery query, CancellationToken ct)
    {
        logger.LogDebug("Searching for active stations with search={Search} and limit={Limit}", query.Search, query.Limit);

        var clamped = Math.Clamp(query.Limit, 1, 100);
        var stations = await repo.SearchActiveStationsAsync(query.Search, clamped, ct);

        return stations;
    }
}
