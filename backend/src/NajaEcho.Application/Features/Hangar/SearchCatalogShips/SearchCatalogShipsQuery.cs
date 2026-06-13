namespace NajaEcho.Application.Features.Hangar.SearchCatalogShips;

public sealed record SearchCatalogShipsQuery(
    Guid UserId,
    string? Search,
    int Page,
    int PageSize);
