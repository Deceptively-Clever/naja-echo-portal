using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;

namespace NajaEcho.Application.Abstractions;

public interface IWarehouseInventoryRepository
{
    Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(
        string? name, string? type, string? subtype, Guid? ownerUserId, string? location,
        CancellationToken ct);

    Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct);

    Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(
        string? search, int limit, CancellationToken ct);

    /// <summary>
    /// Adds a new inventory row or increments an existing matching row's quantity.
    /// Returns the resulting row and whether it was newly created (true) or incremented (false).
    /// </summary>
    Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(
        Guid itemId, Guid ownerUserId, string location, int quantity, int quality,
        Guid? locationId, string? locationType, CancellationToken ct);

    Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct);

    Task<InventoryRowDto> UpdateItemAsync(Guid id, Guid ownerUserId, Guid locationId, string locationType, int quantity, CancellationToken ct);

    Task UpdateLocationAsync(Guid id, Guid locationId, string locationType, CancellationToken ct);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    Task RemoveAsync(Guid id, CancellationToken ct);
}
