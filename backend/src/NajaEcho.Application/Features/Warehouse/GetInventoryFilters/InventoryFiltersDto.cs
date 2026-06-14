namespace NajaEcho.Application.Features.Warehouse.GetInventoryFilters;

public sealed record InventoryFiltersDto(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Subtypes,
    IReadOnlyList<OwnerOption> Owners);
