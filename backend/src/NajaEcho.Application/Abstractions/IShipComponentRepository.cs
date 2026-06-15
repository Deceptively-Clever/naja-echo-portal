using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Application.Abstractions;

public interface IShipComponentRepository
{
    Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(GetShipComponentsQuery query, CancellationToken ct);

    Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct);

    Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(string? search, int limit, CancellationToken ct);

    Task<bool> HasCachedAttributesAsync(Guid itemId, CancellationToken ct);

    Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attributes, CancellationToken ct);
}
