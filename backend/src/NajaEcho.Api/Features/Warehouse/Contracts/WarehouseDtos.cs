namespace NajaEcho.Api.Features.Warehouse.Contracts;

// ── Response records ──────────────────────────────────────────────────────────

public sealed record InventoryRowResponse(
    Guid Id,
    Guid ItemId,
    string Name,
    string? Type,
    string? Subtype,
    int Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location);

public sealed record OwnerOptionResponse(Guid UserId, string DisplayName);

public sealed record InventoryListResponse(IReadOnlyList<InventoryRowResponse> Items);

public sealed record InventoryFiltersResponse(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Subtypes,
    IReadOnlyList<OwnerOptionResponse> Owners);

public sealed record CatalogItemResponse(
    Guid ItemId,
    string Name,
    string? Type,
    string? Subtype);

public sealed record CatalogItemsResponse(IReadOnlyList<CatalogItemResponse> Items);

// ── Request records ───────────────────────────────────────────────────────────

public sealed record AddInventoryItemRequest(
    Guid ItemId,
    Guid? OwnerUserId,
    string Location,
    int Quantity,
    int? Quality);

public sealed record ChangeInventoryQuantityRequest(int Quantity);
