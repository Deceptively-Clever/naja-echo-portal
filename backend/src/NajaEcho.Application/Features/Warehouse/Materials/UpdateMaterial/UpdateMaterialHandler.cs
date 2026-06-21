using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

namespace NajaEcho.Application.Features.Warehouse.Materials.UpdateMaterial;

public sealed class UpdateMaterialHandler(
    IMaterialInventoryRepository repository,
    ILogger<UpdateMaterialHandler> logger)
{
    public async Task<MaterialRowDto> HandleAsync(UpdateMaterialCommand command, CancellationToken ct)
    {
        if (command.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be greater than 0.");
        }

        logger.LogInformation("UpdateMaterial rowId={Id} ownerUserId={OwnerUserId} stationId={StationId} quantity={Quantity}",
            command.Id, command.OwnerUserId, command.StationId, command.Quantity);

        var row = await repository.UpdateMaterialAsync(command.Id, command.OwnerUserId, command.StationId, command.Quantity, ct);

        logger.LogInformation("UpdateMaterial succeeded rowId={Id} newQuantity={Quantity} newOwner={OwnerUserId} newStation={StationId}",
            row.Id, row.Quantity, row.OwnerUserId, row.StationId);

        return row;
    }
}
