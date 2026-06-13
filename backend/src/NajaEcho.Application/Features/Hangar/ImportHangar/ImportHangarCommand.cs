namespace NajaEcho.Application.Features.Hangar.ImportHangar;

public sealed record ImportHangarCommand(
    Guid UserId,
    IReadOnlyList<ImportShipRecord> Items);
