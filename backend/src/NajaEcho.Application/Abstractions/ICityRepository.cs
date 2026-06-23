using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public record CityDto(Guid Id, string Name);

public interface ICityRepository
{
    Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(
        IReadOnlyList<JsonDocument> records,
        IReadOnlyDictionary<int, Guid> starSystemMap,
        CancellationToken ct = default);

    Task<IReadOnlyList<CityDto>> SearchActiveCitiesAsync(
        string? search,
        int limit,
        CancellationToken ct = default);
}
