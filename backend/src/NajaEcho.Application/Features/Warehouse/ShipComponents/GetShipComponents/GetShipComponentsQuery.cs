namespace NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;

public sealed record GetShipComponentsQuery(
    string? Name,
    IReadOnlyList<string>? Types,
    IReadOnlyList<string>? Classes,
    IReadOnlyList<int>? Sizes,
    IReadOnlyList<string>? Grades,
    IReadOnlyList<Guid>? OwnerUserIds,
    IReadOnlyList<string>? Locations,
    bool UnknownClass,
    bool UnknownSize,
    bool UnknownGrade);
