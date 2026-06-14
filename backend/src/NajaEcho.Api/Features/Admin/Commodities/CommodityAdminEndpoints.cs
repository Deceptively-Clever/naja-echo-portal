using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Admin.Commodities.Contracts;
using NajaEcho.Application.Features.Commodities.GetCommodities;
using NajaEcho.Application.Features.Commodities.ImportCommodities;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.Commodities;

namespace NajaEcho.Api.Features.Admin.Commodities;

public static class CommodityAdminEndpoints
{
    public static IEndpointRouteBuilder MapCommodityAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/commodities").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapPost("/import", ImportCommodities);
        group.MapGet("/", GetCommodities);

        return app;
    }

    private static async Task<IResult> GetCommodities(
        GetCommoditiesHandler handler,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        // Paging is normalized in the handler (the use-case boundary); the endpoint stays thin.
        var result = await handler.HandleAsync(new GetCommoditiesQuery(page, pageSize), ct);

        var items = result.Items.Select(c => new CommodityListItemResponse(
            c.Id, c.UexId, c.Name, c.Code, c.Kind,
            c.Status == CommodityStatus.SoftDeleted ? "softDeleted" : "active")).ToList();

        return Results.Ok(new PagedCommoditiesResponse(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> ImportCommodities(
        ImportCommoditiesHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new ImportCommoditiesCommand(), ct);

            var response = new ImportCommoditiesResponse(
                result.Fetched, result.Skipped,
                result.Inserted, result.Updated, result.Unchanged, result.Restored, result.SoftDeleted,
                result.StartedAt, result.CompletedAt, result.DurationMs,
                result.Warning);

            if (result.Fetched == 0)
                return Results.Accepted(value: response);

            return Results.Ok(response);
        }
        catch (ImportAlreadyInProgressException)
        {
            return Results.Conflict(new { title = "Import already in progress.", status = 409 });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: "Failed to fetch the UEX commodity feed.",
                statusCode: StatusCodes.Status502BadGateway,
                title: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Invalid response from UEX commodity feed.");
        }
    }
}
