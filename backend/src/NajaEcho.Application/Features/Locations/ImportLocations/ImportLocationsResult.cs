namespace NajaEcho.Application.Features.Locations.ImportLocations;

public record EntityImportCounts(int Added, int Updated, int Reactivated, int SoftDeleted, int Total);

public record StationImportCounts(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total)
    : EntityImportCounts(Added, Updated, Reactivated, SoftDeleted, Total);

public record CityImportCounts(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total)
    : EntityImportCounts(Added, Updated, Reactivated, SoftDeleted, Total);

public sealed record ImportLocationsResult(
    EntityImportCounts StarSystems,
    StationImportCounts SpaceStations,
    CityImportCounts Cities);
