namespace NajaEcho.Application.Features.Warehouse.GetStations;

public sealed record GetStationsQuery(string? Search, int Limit = 25);
