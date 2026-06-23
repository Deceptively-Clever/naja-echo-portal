namespace NajaEcho.Application.Features.Warehouse.GetLocations;

public sealed record GetLocationsQuery(string? Search, int Limit = 25);
