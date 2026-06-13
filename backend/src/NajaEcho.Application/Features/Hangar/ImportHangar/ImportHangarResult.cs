namespace NajaEcho.Application.Features.Hangar.ImportHangar;

public sealed record ImportHangarResult(
    int TotalRecords,
    int ImportedShips,
    int UnmatchedRecords,
    IReadOnlyList<string> UnmatchedShipNames);
