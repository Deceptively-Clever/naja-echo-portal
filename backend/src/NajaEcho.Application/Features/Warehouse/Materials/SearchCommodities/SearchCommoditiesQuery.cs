namespace NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;

public sealed record SearchCommoditiesQuery(string? Search, int Limit = 25);
