using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IStarSystemRepository
{
    Task<(int added, int updated, int reactivated, int softDeleted)> BulkUpsertAsync(
        IReadOnlyList<JsonDocument> records,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, Guid>> GetActiveUexIdToIdMapAsync(CancellationToken ct = default);
}
