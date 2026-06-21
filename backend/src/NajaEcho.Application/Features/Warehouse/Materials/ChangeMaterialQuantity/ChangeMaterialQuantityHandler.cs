using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

namespace NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;

public sealed class ChangeMaterialQuantityHandler(
    IMaterialInventoryRepository repository,
    ILogger<ChangeMaterialQuantityHandler> logger)
{
    public async Task<MaterialRowDto> HandleAsync(ChangeMaterialQuantityCommand command, CancellationToken ct)
    {
        var quantity = Math.Round(command.Quantity, 3, MidpointRounding.AwayFromZero);
        if (quantity <= 0.000m)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be greater than 0.000.");
        }

        logger.LogInformation("ChangeMaterialQuantity rowId={Id} quantity={Quantity}", command.Id, quantity);

        var row = await repository.UpdateQuantityAsync(command.Id, quantity, ct);

        logger.LogInformation("ChangeMaterialQuantity succeeded rowId={Id} newQuantity={Quantity}", row.Id, row.Quantity);
        return row;
    }
}
