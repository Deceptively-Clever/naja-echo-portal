using NajaEcho.Api.Authorization;
using NajaEcho.Api.Features.Admin.Locations.Contracts;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Locations.ImportLocations;

namespace NajaEcho.Api.Features.Admin.Locations;

public static class LocationAdminEndpoints
{
    public static IEndpointRouteBuilder MapLocationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/locations").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapPost("/import", ImportLocations);

        return app;
    }

    private static async Task<IResult> ImportLocations(
        ImportLocationsHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new ImportLocationsCommand(), ct);

            return Results.Ok(new ImportLocationsResponse(
                new EntityImportCountsResponse(
                    result.StarSystems.Added,
                    result.StarSystems.Updated,
                    result.StarSystems.Reactivated,
                    result.StarSystems.SoftDeleted,
                    result.StarSystems.Total),
                new StationImportCountsResponse(
                    result.SpaceStations.Added,
                    result.SpaceStations.Updated,
                    result.SpaceStations.Reactivated,
                    result.SpaceStations.SoftDeleted,
                    result.SpaceStations.Skipped,
                    result.SpaceStations.Total),
                new CityImportCountsResponse(
                    result.Cities.Added,
                    result.Cities.Updated,
                    result.Cities.Reactivated,
                    result.Cities.SoftDeleted,
                    result.Cities.Skipped,
                    result.Cities.Total)));
        }
        catch (ImportAlreadyInProgressException)
        {
            return Results.Conflict(new { title = "Import already in progress.", status = 409 });
        }
        catch (EmptySourceException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Source returned empty data");
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "UEX source unreachable");
        }
    }
}
