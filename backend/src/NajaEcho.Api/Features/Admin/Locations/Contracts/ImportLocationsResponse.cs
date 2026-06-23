namespace NajaEcho.Api.Features.Admin.Locations.Contracts;

public sealed record EntityImportCountsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Total);

public sealed record StationImportCountsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total);

public sealed record CityImportCountsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total);

public sealed record ImportLocationsResponse(
    EntityImportCountsResponse StarSystems,
    StationImportCountsResponse SpaceStations,
    CityImportCountsResponse Cities);
