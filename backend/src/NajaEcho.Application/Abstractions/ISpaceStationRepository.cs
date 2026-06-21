using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public record StationDto(Guid Id, string Name);

public interface ISpaceStationRepository
{
    Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(
        IReadOnlyList<JsonDocument> records,
        IReadOnlyDictionary<int, Guid> starSystemMap,
        CancellationToken ct = default);

    Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(
        string? search,
        int limit,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
