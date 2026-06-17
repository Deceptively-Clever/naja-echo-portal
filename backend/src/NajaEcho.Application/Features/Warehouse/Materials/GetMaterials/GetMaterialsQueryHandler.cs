using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

public sealed class GetMaterialsQueryHandler(
    IMaterialInventoryRepository repository,
    ILogger<GetMaterialsQueryHandler> logger)
{
    public async Task<IReadOnlyList<MaterialRowDto>> HandleAsync(GetMaterialsQuery query, CancellationToken ct)
    {
        logger.LogInformation(
            "GetMaterials material={Material} owner={Owner} location={Location} qualityMin={QualityMin} qualityMax={QualityMax}",
            query.Material, query.OwnerUserId, query.Location, query.QualityMin, query.QualityMax);

        var rows = await repository.GetMaterialsAsync(
            query.Material, query.OwnerUserId, query.Location, query.QualityMin, query.QualityMax, ct);

        logger.LogInformation("GetMaterials returned {Count} rows", rows.Count);
        return rows;
    }
}
