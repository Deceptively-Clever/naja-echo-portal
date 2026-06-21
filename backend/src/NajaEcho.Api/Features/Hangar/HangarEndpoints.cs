using System.Security.Claims;
using NajaEcho.Api.Features.Hangar.Contracts;
using NajaEcho.Application.Features.Hangar.AddShipToHangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.ImportHangar;
using NajaEcho.Application.Features.Hangar.RemoveShipFromHangar;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using Serilog;

namespace NajaEcho.Api.Features.Hangar;

public static class HangarEndpoints
{
    public static IEndpointRouteBuilder MapHangarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hangar").RequireAuthorization();

        group.MapGet("/mine", GetMyHangar);
        group.MapPost("/mine", AddShipToHangar);
        group.MapDelete("/mine/{shipId:guid}", RemoveShipFromHangar);
        group.MapGet("/org", GetOrgHangar);
        group.MapGet("/org/members", GetOwningMembers);
        group.MapGet("/catalog/search", SearchCatalogShips);
        group.MapPost("/mine/import", ImportHangar);

        return app;
    }

    private static async Task<IResult> GetMyHangar(
        ClaimsPrincipal user,
        GetMyHangarHandler handler,
        string? search = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        Log.Information("GetMyHangar {UserId} search={Search} page={Page}", userId, search, page);

        var result = await handler.HandleAsync(new GetMyHangarQuery(userId, search, page, pageSize), ct);

        var dto = new PagedHangarShipCardsResponse(
            result.Items.Select(Map).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);

        Log.Information("GetMyHangar {UserId} returned {Count}/{Total}", userId, result.Items.Count, result.TotalCount);
        return Results.Ok(dto);
    }

    private static async Task<IResult> AddShipToHangar(
        ClaimsPrincipal user,
        AddShipRequestDto body,
        AddShipToHangarHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        Log.Information("AddShipToHangar {UserId} shipId={ShipId}", userId, body.ShipId);

        try
        {
            var card = await handler.HandleAsync(new AddShipToHangarCommand(userId, body.ShipId), ct);
            Log.Information("AddShipToHangar succeeded {UserId} shipId={ShipId}", userId, body.ShipId);
            return Results.Created($"/api/hangar/mine/{body.ShipId}", Map(card));
        }
        catch (ShipNotFoundException ex)
        {
            Log.Warning("AddShipToHangar 404 {UserId} {ShipId}: {Message}", userId, body.ShipId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Ship not found.");
        }
        catch (ShipAlreadyOwnedException ex)
        {
            Log.Warning("AddShipToHangar 409 {UserId} {ShipId}: {Message}", userId, body.ShipId, ex.Message);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Ship already in hangar.");
        }
    }

    private static async Task<IResult> RemoveShipFromHangar(
        ClaimsPrincipal user,
        Guid shipId,
        RemoveShipFromHangarHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        Log.Information("RemoveShipFromHangar {UserId} shipId={ShipId}", userId, shipId);

        try
        {
            await handler.HandleAsync(new RemoveShipFromHangarCommand(userId, shipId), ct);
            Log.Information("RemoveShipFromHangar succeeded {UserId} shipId={ShipId}", userId, shipId);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RemoveShipFromHangar failed {UserId} shipId={ShipId}", userId, shipId);
            return Results.Problem(detail: "Removal failed; the ship remains in your hangar.",
                statusCode: StatusCodes.Status500InternalServerError, title: "Removal failed.");
        }
    }

    private static async Task<IResult> GetOrgHangar(
        ClaimsPrincipal user,
        GetOrgHangarHandler handler,
        string? search = null,
        bool mine = false,
        Guid? memberId = null,
        int page = 1,
        int pageSize = 25,
        string sortBy = "ownerCount",
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var validatedSortBy = sortBy == "name" ? "name" : "ownerCount";

        Log.Information("GetOrgHangar {UserId} search={Search} mine={Mine} memberId={MemberId} page={Page} sortBy={SortBy}",
            userId, search, mine, memberId, page, validatedSortBy);

        var result = await handler.HandleAsync(
            new GetOrgHangarQuery(userId, search, mine, memberId, page, pageSize, validatedSortBy), ct);

        var dto = new PagedOrgHangarShipCardsResponse(
            result.Items.Select(MapOrg).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);

        Log.Information("GetOrgHangar {UserId} returned {Count}/{Total}", userId, result.Items.Count, result.TotalCount);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetOwningMembers(
        ClaimsPrincipal user,
        GetOwningMembersHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        Log.Information("GetOwningMembers {UserId}", userId);

        var members = await handler.HandleAsync(new GetOwningMembersQuery(), ct);
        var dto = members.Select(m => new OwningMemberDto(m.UserId, m.DisplayName)).ToList();

        Log.Information("GetOwningMembers {UserId} returned {Count}", userId, dto.Count);
        return Results.Ok(dto);
    }

    private static async Task<IResult> SearchCatalogShips(
        ClaimsPrincipal user,
        SearchCatalogShipsHandler handler,
        string? search = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        Log.Information("SearchCatalogShips {UserId} search={Search} page={Page}", userId, search, page);

        var result = await handler.HandleAsync(
            new SearchCatalogShipsQuery(userId, search, page, pageSize), ct);

        var dto = new PagedCatalogSearchItemsResponse(
            result.Items.Select(MapCatalog).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);

        return Results.Ok(dto);
    }

    private static async Task<IResult> ImportHangar(
        ClaimsPrincipal user,
        ImportHangarRequestDto? body,
        ImportHangarHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        if (body?.Items is null)
        {
            return Results.Problem(detail: "items is required.", statusCode: StatusCodes.Status400BadRequest, title: "Bad request.");
        }

        Log.Information("ImportHangar {UserId} items={Count}", userId, body.Items.Count);

        var command = new ImportHangarCommand(
            userId,
            body.Items.Select(i => new ImportShipRecord(i.Name, i.ShipName, i.Unidentified)).ToList());

        var result = await handler.HandleAsync(command, ct);

        Log.Information("ImportHangar {UserId} imported={Imported} unmatched={Unmatched}",
            userId, result.ImportedShips, result.UnmatchedRecords);

        return Results.Ok(new ImportHangarResultDto(
            result.TotalRecords, result.ImportedShips, result.UnmatchedRecords, result.UnmatchedShipNames));
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static HangarShipCardDto Map(Application.Features.Hangar.GetMyHangar.ShipCard c) =>
        new (c.ShipId, c.Name, c.CompanyName, c.UrlPhoto, c.Scu, c.Crew);

    private static OrgHangarShipCardDto MapOrg(Application.Features.Hangar.GetOrgHangar.OrgShipCard c) =>
        new (c.ShipId, c.Name, c.CompanyName, c.UrlPhoto, c.Scu, c.Crew, c.OwnerCount,
            c.Owners.Select(o => new HangarOwnerDto(o.UserId, o.DisplayName)).ToList());

    private static CatalogSearchItemDto MapCatalog(Application.Features.Hangar.SearchCatalogShips.CatalogSearchRow r) =>
        new (r.ShipId, r.Name, r.CompanyName, r.UrlPhoto, r.Scu, r.Crew, r.AlreadyOwned);
}
