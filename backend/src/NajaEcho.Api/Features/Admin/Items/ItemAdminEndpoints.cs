using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Admin.Items.Contracts;
using NajaEcho.Application.Features.ItemCategories.GetCategories;
using NajaEcho.Application.Features.ItemCategories.RefreshCategories;
using NajaEcho.Application.Features.Items.ImportItems;
using NajaEcho.Application.Features.Ships.ImportShips;

namespace NajaEcho.Api.Features.Admin.Items;

public static class ItemAdminEndpoints
{
    public static IEndpointRouteBuilder MapItemAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/items").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapGet("/categories", GetCategories);
        group.MapPost("/categories/refresh", RefreshCategories);
        group.MapPost("/import", ImportItems);

        return app;
    }

    private static async Task<IResult> GetCategories(
        GetCategoriesHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetCategoriesQuery(), ct);

        var categories = result.Items.Select(i => new CategoryListItemResponse(
            i.UexId, i.Name, i.Section, i.Type,
            i.IsGameRelated, i.IsMining, i.SourceDateModified,
            i.LocalItemCount, i.LastImportedAt)).ToList();

        return Results.Ok(new CategoryListResponse(categories, result.LastRefreshedAt));
    }

    private static async Task<IResult> RefreshCategories(
        RefreshCategoriesHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new RefreshCategoriesCommand(), ct);

            return Results.Ok(new RefreshCategoriesResponse(
                result.Fetched, result.Inserted, result.Updated, result.Unchanged,
                result.Failed, result.StartedAt, result.CompletedAt, result.DurationMs));
        }
        catch (ImportAlreadyInProgressException)
        {
            return Results.Conflict(new { title = "An import or refresh is already in progress.", status = 409 });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: "Failed to fetch the UEX categories feed.",
                statusCode: StatusCodes.Status502BadGateway,
                title: ex.Message);
        }
    }

    private static async Task<IResult> ImportItems(
        ImportItemsHandler handler,
        ImportItemsRequest? request,
        CancellationToken ct)
    {
        try
        {
            var command = new ImportItemsCommand(request?.CategoryUexId);
            var result = await handler.HandleAsync(command, ct);

            var errors = result.Errors
                .Select(e => new CategoryImportErrorResponse(e.CategoryUexId, e.CategoryName, e.Message))
                .ToList();

            return Results.Ok(new ImportItemsResponse(
                result.Status.ToString(),
                result.CategoriesProcessed, result.CategoriesSucceeded, result.CategoriesFailed,
                result.ItemsFetched, result.ItemsInserted, result.ItemsUpdated, result.ItemsUnchanged,
                result.ItemsSkippedNoUuid, result.ItemsSoftDeleted, result.ItemsFailed,
                result.StartedAt, result.CompletedAt, result.DurationMs,
                errors));
        }
        catch (ImportAlreadyInProgressException)
        {
            return Results.Conflict(new { title = "An import or refresh is already in progress.", status = 409 });
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: "Failed to fetch items from the UEX API.",
                statusCode: StatusCodes.Status502BadGateway,
                title: ex.Message);
        }
    }
}

public sealed record ImportItemsRequest(int? CategoryUexId);
