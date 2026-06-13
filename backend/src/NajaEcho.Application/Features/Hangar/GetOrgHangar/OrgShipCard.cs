namespace NajaEcho.Application.Features.Hangar.GetOrgHangar;

public sealed record OrgShipCard(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew,
    int OwnerCount,
    IReadOnlyList<HangarOwner> Owners);
