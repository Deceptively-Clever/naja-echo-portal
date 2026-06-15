using System.Security.Claims;
using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Warehouse.Contracts;
using NajaEcho.Application.Features.Warehouse.AddInventoryItem;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
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
        group.MapGet("/items", GetInventory);
        group.MapPost("/items", AddInventoryItem).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapGet("/items/filters", GetInventoryFilters);
        group.MapGet("/catalog/search", SearchCatalogItems).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapPut("/items/{id:guid}/quantity", ChangeInventoryQuantity).RequireAuthorization(AuthorizationPolicies.Quartermaster);
        group.MapDelete("/items/{id:guid}", RemoveInventoryItem).RequireAuthorization(AuthorizationPolicies.Quartermaster);

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        Log.Information("GetShipComponents {CallerId} name={Name}", callerId, name);

        var query = new GetShipComponentsQuery(name, type, @class, size, grade, ownerUserId, location, unknownClass, unknownSize, unknownGrade);
        var rows = await handler.HandleAsync(query, ct);
        var dto = new ShipComponentListResponse(rows.Select(r =>
            new ShipComponentRowResponse(r.Id, r.ItemId, r.Name, r.Type, r.Class, r.Size, r.Grade, r.Quantity, r.OwnerUserId, r.OwnerDisplayName, r.Location)).ToList());

        Log.Information("GetShipComponents {CallerId} returned {Count} rows", callerId, rows.Count);
        return Results.Ok(dto);
    }

    // ── GET /api/warehouse/ship-components/filters ────────────────────────

    private static async Task<IResult> GetShipComponentFilters(
        ClaimsPrincipal user,
        GetShipComponentFiltersQueryHandler handler,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        var ownerUserId = body.OwnerUserId ?? callerId;

        Log.Information("AddInventoryItem {CallerId} itemId={ItemId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity}",
            callerId, body.ItemId, ownerUserId, body.Location, body.Quantity);

        try
        {
            var (row, isNew) = await handler.HandleAsync(
                new AddInventoryItemCommand(body.ItemId, ownerUserId, body.Location, body.Quantity), ct);

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
        catch (OwnerNotFoundException ex)
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

    // ── PUT /api/warehouse/items/{id}/quantity ────────────────────────────

    private static async Task<IResult> ChangeInventoryQuantity(
        ClaimsPrincipal user,
        Guid id,
        ChangeInventoryQuantityRequest body,
        ChangeInventoryQuantityHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

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

    // ── Helpers ──────────────────────────────────────────────────────────

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static InventoryRowResponse MapRow(Application.Features.Warehouse.GetInventory.InventoryRowDto r) =>
        new(r.Id, r.ItemId, r.Name, r.Type, r.Subtype, r.Quantity, r.OwnerUserId, r.OwnerDisplayName, r.Location);

    private static CatalogItemResponse MapCatalog(Application.Features.Warehouse.SearchCatalogItems.CatalogItemResultDto r) =>
        new(r.ItemId, r.Name, r.Type, r.Subtype);
}
