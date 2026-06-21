using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;

namespace NajaEcho.Application.Features.Warehouse.Materials.TransferMaterial;

public sealed class TransferMaterialHandler(
    IMaterialInventoryRepository repo,
    ISpaceStationRepository stationRepo,
    ILogger<TransferMaterialHandler> logger)
{
    public async Task HandleAsync(TransferMaterialCommand cmd, CancellationToken ct)
    {
        var stationExists = await stationRepo.ExistsAsync(cmd.StationId, ct);
        if (!stationExists)
            throw new InvalidOperationException($"Station with id {cmd.StationId} not found.");

        var rowExists = await repo.ExistsAsync(cmd.RowId, ct);
        if (!rowExists)
            throw new MaterialRowNotFoundException(cmd.RowId);

        await repo.UpdateStationAsync(cmd.RowId, cmd.StationId, ct);

        logger.LogInformation("TransferMaterial rowId={RowId} stationId={StationId}", cmd.RowId, cmd.StationId);
    }
}
