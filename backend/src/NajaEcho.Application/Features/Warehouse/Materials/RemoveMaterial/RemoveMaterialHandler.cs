using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.Materials.RemoveMaterial;

public sealed class RemoveMaterialHandler(
    IMaterialInventoryRepository repository,
    ILogger<RemoveMaterialHandler> logger)
{
    public async Task HandleAsync(RemoveMaterialCommand command, CancellationToken ct)
    {
        logger.LogInformation("RemoveMaterial rowId={Id}", command.Id);
        await repository.RemoveAsync(command.Id, ct);
        logger.LogInformation("RemoveMaterial succeeded rowId={Id}", command.Id);
    }
}
