namespace NajaEcho.Application.Features.Warehouse.SearchCatalogItems;

public sealed record SearchCatalogItemsQuery(string? Search, int Limit = 25);
