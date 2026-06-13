using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Features.Ships.ImportShips;

public sealed class ImportShipsHandler(
    IShipRepository repository,
    IUexVehicleClient vehicleClient,
    IImportCoordinator coordinator,
    ILogger<ImportShipsHandler> logger)
{
    public async Task<ImportShipsResult> HandleAsync(ImportShipsCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Import ships: acquiring lock");

        if (!coordinator.TryAcquire())
        {
            logger.LogWarning("Import ships: already in progress");
            throw new ImportAlreadyInProgressException();
        }

        try
        {
            logger.LogInformation("Import ships: fetching feed");
            IReadOnlyList<JsonDocument> records;
            try
            {
                records = await vehicleClient.FetchAllVehiclesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import ships: feed fetch failed");
                throw;
            }

            logger.LogInformation("Import ships: source record count = {Count}", records.Count);

            if (records.Count == 0)
            {
                const string warning = "Feed returned zero records; no changes applied.";
                logger.LogWarning("Import ships: {Warning}", warning);
                return new ImportShipsResult(0, 0, 0, 0, 0, warning);
            }

            var ships = records.Select(MapToShip).ToList();

            var (added, updated, reactivated, softDeleted) = await repository.BulkUpsertAsync(ships, ct);

            logger.LogInformation(
                "Import ships: completed — added={Added} updated={Updated} reactivated={Reactivated} softDeleted={SoftDeleted}",
                added, updated, reactivated, softDeleted);

            return new ImportShipsResult(added, updated, reactivated, softDeleted, records.Count);
        }
        catch (ImportAlreadyInProgressException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import ships: failed, transaction rolled back");
            throw;
        }
        finally
        {
            coordinator.Release();
        }
    }

    private static Ship MapToShip(JsonDocument doc)
    {
        var el = doc.RootElement;

        return new Ship
        {
            UexId = el.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            Uuid = el.TryGetProperty("uuid", out var uuid) ? uuid.GetString() : null,
            Name = el.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            NameFull = el.TryGetProperty("name_full", out var nf) ? nf.GetString() : null,
            CompanyName = el.TryGetProperty("company_name", out var cn) ? cn.GetString() : null,
            RawData = doc,
        };
    }
}

public sealed class ImportAlreadyInProgressException() : Exception("An import is already in progress.");
