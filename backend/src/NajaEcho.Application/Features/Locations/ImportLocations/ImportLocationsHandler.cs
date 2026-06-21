using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Locations.ImportLocations;

public sealed class ImportAlreadyInProgressException() : Exception("An import is already in progress.");

public sealed class ImportLocationsHandler(
    IUexLocationClient uexClient,
    IStarSystemRepository starSystemRepo,
    ISpaceStationRepository stationRepo,
    IImportCoordinator coordinator,
    ILogger<ImportLocationsHandler> logger)
{
    public async Task<ImportLocationsResult> HandleAsync(ImportLocationsCommand cmd, CancellationToken ct)
    {
        if (!coordinator.TryAcquire())
        {
            throw new ImportAlreadyInProgressException();
        }

        try
        {
            var starSystemDocs = await uexClient.FetchAllStarSystemsAsync(ct);
            if (!starSystemDocs.Any())
            {
                throw new EmptySourceException("star systems");
            }

            var systemCounts = await starSystemRepo.BulkUpsertAsync(starSystemDocs, ct);
            var starSystemMap = await starSystemRepo.GetActiveUexIdToIdMapAsync(ct);

            var stationDocs = await uexClient.FetchAllSpaceStationsAsync(ct);
            if (!stationDocs.Any())
            {
                throw new EmptySourceException("space stations");
            }

            var stationCounts = await stationRepo.BulkUpsertAsync(stationDocs, starSystemMap, ct);

            logger.LogInformation("Locations import complete: systems={@SystemCounts} stations={@StationCounts}", systemCounts, stationCounts);

            var systemResult = new EntityImportCounts(
                systemCounts.added,
                systemCounts.updated,
                systemCounts.reactivated,
                systemCounts.softDeleted,
                systemCounts.added + systemCounts.updated + systemCounts.reactivated + systemCounts.softDeleted);

            var stationResult = new StationImportCounts(
                stationCounts.added,
                stationCounts.updated,
                stationCounts.reactivated,
                stationCounts.softDeleted,
                stationCounts.skipped,
                stationCounts.added + stationCounts.updated + stationCounts.reactivated + stationCounts.softDeleted);

            return new ImportLocationsResult(systemResult, stationResult);
        }
        finally
        {
            coordinator.Release();
        }
    }
}
