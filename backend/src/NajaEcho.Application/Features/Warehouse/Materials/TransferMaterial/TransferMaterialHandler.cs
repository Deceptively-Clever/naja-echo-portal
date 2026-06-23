using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;

namespace NajaEcho.Application.Features.Warehouse.Materials.TransferMaterial;

public sealed class TransferMaterialHandler(
    IMaterialInventoryRepository repo,
    ILogger<TransferMaterialHandler> logger)
{
    public async Task HandleAsync(TransferMaterialCommand cmd, CancellationToken ct)
    {
        var rowExists = await repo.ExistsAsync(cmd.RowId, ct);
        if (!rowExists)
        {
            throw new MaterialRowNotFoundException(cmd.RowId);
        }

        await repo.UpdateLocationAsync(cmd.RowId, cmd.LocationId, cmd.LocationType, ct);

        logger.LogInformation("TransferMaterial rowId={RowId} locationId={LocationId} locationType={LocationType}",
            cmd.RowId, cmd.LocationId, cmd.LocationType);
    }
}
