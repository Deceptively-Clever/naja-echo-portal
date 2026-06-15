namespace NajaEcho.Api.Features.Warehouse.Contracts;

// ── Response records ──────────────────────────────────────────────────────────

public sealed record ShipComponentRowResponse(
    Guid Id,
    Guid ItemId,
    string Name,
    string? Type,
    string? Class,
    int? Size,
    string? Grade,
    int Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location);

public sealed record ShipComponentListResponse(IReadOnlyList<ShipComponentRowResponse> Items);

public sealed record ShipComponentOwnerOption(Guid UserId, string DisplayName);

public sealed record ShipComponentFiltersResponse(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Classes,
    IReadOnlyList<int> Sizes,
    IReadOnlyList<string> Grades,
    IReadOnlyList<ShipComponentOwnerOption> Owners,
    IReadOnlyList<string> Locations,
    bool UnknownClass,
    bool UnknownSize,
    bool UnknownGrade);

public sealed record SystemsCatalogItemResponse(Guid ItemId, string Name, string? Type);

public sealed record SystemsCatalogResponse(IReadOnlyList<SystemsCatalogItemResponse> Items);

// ── Request records ───────────────────────────────────────────────────────────

public sealed record AddShipComponentRequest(
    Guid ItemId,
    Guid? OwnerUserId,
    string Location,
    int Quantity,
    int? Quality);
