namespace NajaEcho.Api.Features.Hangar.Contracts;

public sealed record HangarShipCardDto(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew);

public sealed record HangarOwnerDto(Guid UserId, string DisplayName);

public sealed record OrgHangarShipCardDto(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew,
    int OwnerCount,
    IReadOnlyList<HangarOwnerDto> Owners);

public sealed record CatalogSearchItemDto(
    Guid ShipId,
    string Name,
    string? CompanyName,
    string? UrlPhoto,
    decimal? Scu,
    string? Crew,
    bool AlreadyOwned);

public sealed record OwningMemberDto(Guid UserId, string DisplayName);

public sealed record AddShipRequestDto(Guid ShipId);

public sealed record PagedHangarShipCardsResponse(
    IReadOnlyList<HangarShipCardDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PagedOrgHangarShipCardsResponse(
    IReadOnlyList<OrgHangarShipCardDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PagedCatalogSearchItemsResponse(
    IReadOnlyList<CatalogSearchItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
