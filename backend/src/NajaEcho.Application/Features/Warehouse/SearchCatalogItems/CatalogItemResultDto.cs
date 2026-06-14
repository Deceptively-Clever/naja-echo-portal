namespace NajaEcho.Application.Features.Warehouse.SearchCatalogItems;

public sealed record CatalogItemResultDto(
    Guid ItemId,
    string Name,
    string? Type,
    string? Subtype);
