namespace NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;

public sealed record ShipComponentFiltersDto(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Classes,
    IReadOnlyList<int> Sizes,
    IReadOnlyList<string> Grades,
    IReadOnlyList<OwnerFilterOption> Owners,
    IReadOnlyList<string> Locations,
    bool UnknownClass,
    bool UnknownSize,
    bool UnknownGrade);

public sealed record OwnerFilterOption(Guid UserId, string DisplayName);
