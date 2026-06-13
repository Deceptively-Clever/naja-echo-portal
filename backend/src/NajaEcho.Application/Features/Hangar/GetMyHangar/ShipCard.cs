namespace NajaEcho.Application.Features.Hangar.GetMyHangar;

public sealed record ShipCard(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew);
