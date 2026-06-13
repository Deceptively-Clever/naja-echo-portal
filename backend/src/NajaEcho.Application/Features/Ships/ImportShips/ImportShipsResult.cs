namespace NajaEcho.Application.Features.Ships.ImportShips;

public sealed record ImportShipsResult(
    int Added,
    int Updated,
    int Reactivated,
    int SoftDeleted,
    int Total,
    string? Warning = null);
