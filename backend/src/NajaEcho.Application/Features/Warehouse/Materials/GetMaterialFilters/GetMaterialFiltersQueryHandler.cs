using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

namespace NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;

public sealed class GetMaterialFiltersQueryHandler(
    IMaterialInventoryRepository repository,
    ILogger<GetMaterialFiltersQueryHandler> logger)
{
    public async Task<MaterialFiltersDto> HandleAsync(GetMaterialFiltersQuery query, CancellationToken ct)
    {
        logger.LogInformation("GetMaterialFilters");
        return await repository.GetMaterialFiltersAsync(ct);
    }
}
