namespace NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;

public sealed record MaterialFiltersDto(
    IReadOnlyList<OwnerOption> Owners,
    IReadOnlyList<string> Locations);
