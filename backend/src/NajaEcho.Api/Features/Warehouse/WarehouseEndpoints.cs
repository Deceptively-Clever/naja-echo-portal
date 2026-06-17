using System.Security.Claims;
using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Warehouse.Contracts;
using NajaEcho.Application.Features.Warehouse.AddInventoryItem;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.Materials.AddMaterial;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.RemoveMaterial;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using NajaEcho.Application.Features.Warehouse.RemoveInventoryItem;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using Serilog;

namespace NajaEcho.Api.Features.Warehouse;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/warehouse").RequireAuthorization();

        group.MapGet("/ship-components", GetShipComponents);
        group.MapGet("/ship-components/filters", GetShipComponentFilters);
        group.MapGet("/ship-components/catalog/search", SearchSystemsCatalog).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapPost("/ship-components", AddShipComponent).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapGet("/items", GetInventory);
        group.MapPost("/items", AddInventoryItem).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapGet("/items/filters", GetInventoryFilters);
        group.MapGet("/catalog/search", SearchCatalogItems).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapPut("/items/{id:guid}/quantity", ChangeInventoryQuantity).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapDelete("/items/{id:guid}", RemoveInventoryItem).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapGet("/materials", GetMaterials);
        group.MapGet("/materials/filters", GetMaterialFilters);
        group.MapGet("/materials/catalog/search", SearchCommoditiesCatalog).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapPost("/materials", AddMaterial).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapPut("/materials/{id:guid}/quantity", ChangeMaterialQuantity).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapDelete("/materials/{id:guid}", RemoveMaterial).RequireAuthorization(AuthorizationPolicies.Quartermaster);

        return app;
    }

    // ── GET /api/warehouse/ship-components ───────────────────────────────

    private static async Task<IResult> GetShipComponents(
        ClaimsPrincipal user,
        GetShipComponentsQueryHandler handler,
        string? name = null,
        string[]? type = null,
        string[]? @class = null,
        int[]? size = null,
        string[]? grade = null,
        Guid[]? ownerUserId = null,
        string[]? location = null,
        bool unknownClass = false,
        bool unknownSize = false,
        bool unknownGrade = false,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetShipComponents {CallerId} name={Name}", callerId, name);

        var query = new GetShipComponentsQuery(name, type, @class, size, grade, ownerUserId, location, unknownClass, unknownSize, unknownGrade);
        var rows = await handler.HandleAsync(query, ct);
        var dto = new ShipComponentListResponse(rows.Select(r =>
            new ShipComponentRowResponse(r.Id, r.ItemId, r.Name, r.Type, r.Class, r.Size, r.Grade, r.Quantity, r.Quality, r.OwnerUserId, r.OwnerDisplayName, r.Location)).ToList());

        Log.Information("GetShipComponents {CallerId} returned {Count} rows", callerId, rows.Count);
        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/ship-components/filters ────────────────────────

    private static async Task<IResult> GetShipComponentFilters(
        ClaimsPrincipal user,
        GetShipComponentFiltersQueryHandler handler,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetShipComponentFilters {CallerId}", callerId);

        var dto = await handler.HandleAsync(new GetShipComponentFiltersQuery(), ct);
        var response = new ShipComponentFiltersResponse(
            dto.Types,
            dto.Classes,
            dto.Sizes,
            dto.Grades,
            dto.Owners.Select(o => new ShipComponentOwnerOption(o.UserId, o.DisplayName)).ToList(),
            dto.Locations,
            dto.UnknownClass,
            dto.UnknownSize,
            dto.UnknownGrade);

        return Results.Ok(response);
    }

    // ── GET /api/warehouse/ship-components/catalog/search ─────────────────

    private static async Task<IResult> SearchSystemsCatalog(
        ClaimsPrincipal user,
        SearchSystemsCatalogQueryHandler handler,
        string? search = null,
        int limit = 25,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("SearchSystemsCatalog {CallerId} search={Search}", callerId, search);

        var results = await handler.HandleAsync(new SearchSystemsCatalogQuery(search, limit), ct);
        var dto = new SystemsCatalogResponse(results.Select(r => new SystemsCatalogItemResponse(r.ItemId, r.Name, r.Type)).ToList());

        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/items ──────────────────────────────────────────

    private static async Task<IResult> GetInventory(
        ClaimsPrincipal user,
        GetInventoryHandler handler,
        string? name = null,
        string? type = null,
        string? subtype = null,
        Guid? ownerUserId = null,
        string? location = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetInventory {CallerId} name={Name} type={Type} subtype={Subtype} owner={Owner} location={Location}",
            callerId, name, type, subtype, ownerUserId, location);

        var rows = await handler.HandleAsync(new GetInventoryQuery(name, type, subtype, ownerUserId, location), ct);
        var dto = new InventoryListResponse(rows.Select(MapRow).ToList());

        Log.Information("GetInventory {CallerId} returned {Count} rows", callerId, rows.Count);
        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/items/filters ─────────────────────────────────

    private static async Task<IResult> GetInventoryFilters(
        ClaimsPrincipal user,
        GetInventoryFiltersHandler handler,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetInventoryFilters {CallerId}", callerId);

        var filters = await handler.HandleAsync(new GetInventoryFiltersQuery(), ct);
        var dto = new InventoryFiltersResponse(
            filters.Types,
            filters.Subtypes,
            filters.Owners.Select(o => new OwnerOptionResponse(o.UserId, o.DisplayName)).ToList());

        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/catalog/search ────────────────────────────────

    private static async Task<IResult> SearchCatalogItems(
        ClaimsPrincipal user,
        SearchCatalogItemsHandler handler,
        string? search = null,
        int limit = 25,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("SearchCatalogItems {CallerId} search={Search}", callerId, search);

        var results = await handler.HandleAsync(new SearchCatalogItemsQuery(search, Math.Clamp(limit, 1, 100)), ct);
        var dto = new CatalogItemsResponse(results.Select(MapCatalog).ToList());

        return Results.Ok(dto);
    }

    // ── POST /api/warehouse/items ─────────────────────────────────────────

    private static async Task<IResult> AddInventoryItem(
        ClaimsPrincipal user,
        AddInventoryItemRequest body,
        AddInventoryItemHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        var ownerUserId = body.OwnerUserId ?? callerId;
        var quality = body.Quality ?? 500;

        Log.Information("AddInventoryItem {CallerId} itemId={ItemId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity} quality={Quality}",
            callerId, body.ItemId, ownerUserId, body.Location, body.Quantity, quality);

        try
        {
            var (row, isNew) = await handler.HandleAsync(
                new AddInventoryItemCommand(body.ItemId, ownerUserId, body.Location, body.Quantity, quality), ct);

            Log.Information("AddInventoryItem {CallerId} {Action} rowId={RowId}", callerId, isNew ? "created" : "incremented", row.Id);

            return isNew
                ? Results.Created($"/api/warehouse/items/{row.Id}", MapRow(row))
                : Results.Ok(MapRow(row));
        }
        catch (ItemNotFoundException ex)
        {
            Log.Warning("AddInventoryItem 404 {CallerId} itemId={ItemId}: {Message}", callerId, body.ItemId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Item not found.");
        }
        catch (NajaEcho.Application.Features.Warehouse.AddInventoryItem.OwnerNotFoundException ex)
        {
            Log.Warning("AddInventoryItem 404 {CallerId} ownerUserId={OwnerId}: {Message}", callerId, ownerUserId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Owner not found.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning("AddInventoryItem 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
        catch (ArgumentException ex)
        {
            Log.Warning("AddInventoryItem 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
    }

    // ── POST /api/warehouse/ship-components ────────────────────────────────

    private static async Task<IResult> AddShipComponent(
        ClaimsPrincipal user,
        AddShipComponentRequest body,
        AddInventoryItemHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        var ownerUserId = body.OwnerUserId ?? callerId;
        var quality = body.Quality ?? 500;

        Log.Information("AddShipComponent {CallerId} itemId={ItemId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity} quality={Quality}",
            callerId, body.ItemId, ownerUserId, body.Location, body.Quantity, quality);

        try
        {
            var (row, isNew) = await handler.HandleAsync(
                new AddInventoryItemCommand(body.ItemId, ownerUserId, body.Location, body.Quantity, quality), ct);

            Log.Information("AddShipComponent {CallerId} {Action} rowId={RowId}", callerId, isNew ? "created" : "incremented", row.Id);

            return isNew
                ? Results.Created($"/api/warehouse/items/{row.Id}", MapRow(row))
                : Results.Ok(MapRow(row));
        }
        catch (ItemNotFoundException ex)
        {
            Log.Warning("AddShipComponent 404 {CallerId} itemId={ItemId}: {Message}", callerId, body.ItemId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Item not found.");
        }
        catch (NajaEcho.Application.Features.Warehouse.AddInventoryItem.OwnerNotFoundException ex)
        {
            Log.Warning("AddShipComponent 404 {CallerId} ownerUserId={OwnerId}: {Message}", callerId, ownerUserId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Owner not found.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning("AddShipComponent 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
        catch (ArgumentException ex)
        {
            Log.Warning("AddShipComponent 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
    }

    // ── PUT /api/warehouse/items/{id}/quantity ────────────────────────────

    private static async Task<IResult> ChangeInventoryQuantity(
        ClaimsPrincipal user,
        Guid id,
        ChangeInventoryQuantityRequest body,
        ChangeInventoryQuantityHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("ChangeInventoryQuantity {CallerId} rowId={Id} quantity={Quantity}", callerId, id, body.Quantity);

        try
        {
            var row = await handler.HandleAsync(new ChangeInventoryQuantityCommand(id, body.Quantity), ct);
            Log.Information("ChangeInventoryQuantity {CallerId} succeeded rowId={Id}", callerId, id);
            return Results.Ok(MapRow(row));
        }
        catch (InventoryRowNotFoundException ex)
        {
            Log.Warning("ChangeInventoryQuantity 404 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Inventory row not found.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning("ChangeInventoryQuantity 400 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
    }

    // ── DELETE /api/warehouse/items/{id} ─────────────────────────────────

    private static async Task<IResult> RemoveInventoryItem(
        ClaimsPrincipal user,
        Guid id,
        RemoveInventoryItemHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("RemoveInventoryItem {CallerId} rowId={Id}", callerId, id);

        try
        {
            await handler.HandleAsync(new RemoveInventoryItemCommand(id), ct);
            Log.Information("RemoveInventoryItem {CallerId} succeeded rowId={Id}", callerId, id);
            return Results.NoContent();
        }
        catch (InventoryRowNotFoundException ex)
        {
            Log.Warning("RemoveInventoryItem 404 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Inventory row not found.");
        }
    }

    // ── GET /api/warehouse/materials ──────────────────────────────────────

    private static async Task<IResult> GetMaterials(
        ClaimsPrincipal user,
        GetMaterialsQueryHandler handler,
        string? material = null,
        Guid? ownerUserId = null,
        string? location = null,
        int? qualityMin = null,
        int? qualityMax = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetMaterials {CallerId} material={Material} owner={Owner} location={Location} qualityMin={QualityMin} qualityMax={QualityMax}",
            callerId, material, ownerUserId, location, qualityMin, qualityMax);

        var rows = await handler.HandleAsync(new GetMaterialsQuery(material, ownerUserId, location, qualityMin, qualityMax), ct);
        var dto = new MaterialListResponse(rows.Select(MapMaterialRow).ToList());

        Log.Information("GetMaterials {CallerId} returned {Count} rows", callerId, rows.Count);
        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/materials/filters ───────────────────────────────

    private static async Task<IResult> GetMaterialFilters(
        ClaimsPrincipal user,
        GetMaterialFiltersQueryHandler handler,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("GetMaterialFilters {CallerId}", callerId);

        var dto = await handler.HandleAsync(new GetMaterialFiltersQuery(), ct);
        var response = new MaterialFiltersResponse(
            dto.Owners.Select(o => new OwnerOptionResponse(o.UserId, o.DisplayName)).ToList(),
            dto.Locations);

        return Results.Ok(response);
    }

    // ── GET /api/warehouse/materials/catalog/search ────────────────────────

    private static async Task<IResult> SearchCommoditiesCatalog(
        ClaimsPrincipal user,
        SearchCommoditiesQueryHandler handler,
        string? search = null,
        int limit = 25,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("SearchCommoditiesCatalog {CallerId} search={Search}", callerId, search);

        var results = await handler.HandleAsync(new SearchCommoditiesQuery(search, Math.Clamp(limit, 1, 100)), ct);
        var dto = new CommodityCatalogResponse(results.Select(r => new CommodityCatalogItemResponse(r.CommodityId, r.Name, r.Code)).ToList());

        return Results.Ok(dto);
    }

    // ── POST /api/warehouse/materials ───────────────────────────────────────

    private static async Task<IResult> AddMaterial(
        ClaimsPrincipal user,
        AddMaterialRequest body,
        AddMaterialHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        var ownerUserId = body.OwnerUserId ?? callerId;
        var quality = body.Quality ?? 500;

        Log.Information("AddMaterial {CallerId} commodityId={CommodityId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity} quality={Quality}",
            callerId, body.CommodityId, ownerUserId, body.Location, body.Quantity, quality);

        try
        {
            var (row, isNew) = await handler.HandleAsync(
                new AddMaterialCommand(body.CommodityId, ownerUserId, body.Location, body.Quantity, quality), ct);

            Log.Information("AddMaterial {CallerId} {Action} rowId={RowId}", callerId, isNew ? "created" : "incremented", row.Id);

            return isNew
                ? Results.Created($"/api/warehouse/materials/{row.Id}", MapMaterialRow(row))
                : Results.Ok(MapMaterialRow(row));
        }
        catch (CommodityNotFoundException ex)
        {
            Log.Warning("AddMaterial 404 {CallerId} commodityId={CommodityId}: {Message}", callerId, body.CommodityId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Commodity not found.");
        }
        catch (NajaEcho.Application.Features.Warehouse.Materials.AddMaterial.OwnerNotFoundException ex)
        {
            Log.Warning("AddMaterial 404 {CallerId} ownerUserId={OwnerId}: {Message}", callerId, ownerUserId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Owner not found.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning("AddMaterial 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
        catch (ArgumentException ex)
        {
            Log.Warning("AddMaterial 400 {CallerId}: {Message}", callerId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
    }

    // ── PUT /api/warehouse/materials/{id}/quantity ──────────────────────────

    private static async Task<IResult> ChangeMaterialQuantity(
        ClaimsPrincipal user,
        Guid id,
        ChangeMaterialQuantityRequest body,
        ChangeMaterialQuantityHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("ChangeMaterialQuantity {CallerId} rowId={Id} quantity={Quantity}", callerId, id, body.Quantity);

        try
        {
            var row = await handler.HandleAsync(new ChangeMaterialQuantityCommand(id, body.Quantity), ct);
            Log.Information("ChangeMaterialQuantity {CallerId} succeeded rowId={Id}", callerId, id);
            return Results.Ok(MapMaterialRow(row));
        }
        catch (MaterialRowNotFoundException ex)
        {
            Log.Warning("ChangeMaterialQuantity 404 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Material row not found.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning("ChangeMaterialQuantity 400 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation error.");
        }
    }

    // ── DELETE /api/warehouse/materials/{id} ────────────────────────────────

    private static async Task<IResult> RemoveMaterial(
        ClaimsPrincipal user,
        Guid id,
        RemoveMaterialHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) { return Results.Unauthorized(); }

        Log.Information("RemoveMaterial {CallerId} rowId={Id}", callerId, id);

        try
        {
            await handler.HandleAsync(new RemoveMaterialCommand(id), ct);
            Log.Information("RemoveMaterial {CallerId} succeeded rowId={Id}", callerId, id);
            return Results.NoContent();
        }
        catch (MaterialRowNotFoundException ex)
        {
            Log.Warning("RemoveMaterial 404 {CallerId} rowId={Id}: {Message}", callerId, id, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Material row not found.");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static InventoryRowResponse MapRow(Application.Features.Warehouse.GetInventory.InventoryRowDto r) =>
        new(r.Id, r.ItemId, r.Name, r.Type, r.Subtype, r.Quantity, r.Quality, r.OwnerUserId, r.OwnerDisplayName, r.Location);

    private static CatalogItemResponse MapCatalog(Application.Features.Warehouse.SearchCatalogItems.CatalogItemResultDto r) =>
        new(r.ItemId, r.Name, r.Type, r.Subtype);

    private static MaterialRowResponse MapMaterialRow(Application.Features.Warehouse.Materials.GetMaterials.MaterialRowDto r) =>
        new(r.Id, r.CommodityId, r.MaterialName, r.MaterialCode, r.Quantity, r.Quality, r.OwnerUserId, r.OwnerDisplayName, r.Location);
}
