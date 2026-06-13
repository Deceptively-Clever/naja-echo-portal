using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Admin.Ships.Contracts;
using NajaEcho.Application.Features.Ships.GetShipById;
using NajaEcho.Application.Features.Ships.GetShips;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.Ships;

namespace NajaEcho.Api.Features.Admin.Ships;

public static class ShipAdminEndpoints
{
    public static IEndpointRouteBuilder MapShipAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ships").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapPost("/import", ImportShips);
        group.MapGet("/", GetShips);
        group.MapGet("/{id:guid}", GetShipById);

        return app;
    }

    private static async Task<IResult> ImportShips(
        ImportShipsHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new ImportShipsCommand(), ct);

            if (result.Total == 0)
                return Results.Accepted(value: new ImportShipsResponse(0, 0, 0, 0, 0));

            return Results.Ok(new ImportShipsResponse(
                result.Added, result.Updated, result.Reactivated, result.SoftDeleted, result.Total));
        }
        catch (ImportAlreadyInProgressException)
        {
            return Results.Conflict(new { title = "Import already in progress.", status = 409 });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: "Failed to fetch the UEX vehicle feed.",
                statusCode: StatusCodes.Status502BadGateway,
                title: ex.Message);
        }
    }

    private static async Task<IResult> GetShips(
        GetShipsHandler handler,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(new GetShipsQuery(page, pageSize), ct);

        var items = result.Items.Select(i => new ShipListItemResponse(
            i.Id, i.Name, i.CompanyName,
            i.Status == ShipStatus.SoftDeleted ? "softDeleted" : "active")).ToList();

        return Results.Ok(new PagedShipsResponse(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> GetShipById(
        Guid id,
        GetShipByIdHandler handler,
        CancellationToken ct)
    {
        var detail = await handler.HandleAsync(new GetShipByIdQuery(id), ct);
        if (detail is null)
            return Results.NotFound();

        return Results.Ok(new ShipDetailResponse(
            detail.Id,
            detail.Status == ShipStatus.SoftDeleted ? "softDeleted" : "active",
            detail.Fields));
    }
}
