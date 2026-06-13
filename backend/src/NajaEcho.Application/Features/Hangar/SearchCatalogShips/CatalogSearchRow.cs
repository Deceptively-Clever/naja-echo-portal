namespace NajaEcho.Application.Features.Hangar.SearchCatalogShips;

public sealed record CatalogSearchRow(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew,
    bool AlreadyOwned);
