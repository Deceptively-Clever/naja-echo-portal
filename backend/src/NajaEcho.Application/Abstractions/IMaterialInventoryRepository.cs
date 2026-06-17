using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;

namespace NajaEcho.Application.Abstractions;

public interface IMaterialInventoryRepository
{
    Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
        string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax,
        CancellationToken ct);

    Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct);

    Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(
        string? search, int limit, CancellationToken ct);

    /// <summary>
    /// Adds a new material row or increments an existing matching row's quantity.
    /// Returns the resulting row and whether it was newly created (true) or incremented (false).
    /// </summary>
    Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
        Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, CancellationToken ct);

    Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct);

    Task RemoveAsync(Guid id, CancellationToken ct);
}
