namespace NajaEcho.Api.Features.Warehouse.Contracts;

// ── Response records ──────────────────────────────────────────────────────────

public sealed record MaterialRowResponse(
    Guid Id,
    Guid CommodityId,
    string MaterialName,
    string? MaterialCode,
    decimal Quantity,
    int Quality,
    Guid OwnerUserId,
    string OwnerDisplayName,
    string Location);

public sealed record MaterialListResponse(IReadOnlyList<MaterialRowResponse> Rows);

public sealed record MaterialFiltersResponse(
    IReadOnlyList<OwnerOptionResponse> Owners,
    IReadOnlyList<string> Locations);

public sealed record CommodityCatalogItemResponse(Guid CommodityId, string Name, string? Code);

public sealed record CommodityCatalogResponse(IReadOnlyList<CommodityCatalogItemResponse> Commodities);

// ── Request records ───────────────────────────────────────────────────────────

public sealed record AddMaterialRequest(
    Guid CommodityId,
    Guid? OwnerUserId,
    string Location,
    decimal Quantity,
    int? Quality,
    Guid? StationId = null);

public sealed record ChangeMaterialQuantityRequest(decimal Quantity);
