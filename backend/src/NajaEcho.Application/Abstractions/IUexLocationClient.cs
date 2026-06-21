using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexLocationClient
{
    Task<IReadOnlyList<JsonDocument>> FetchAllStarSystemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<JsonDocument>> FetchAllSpaceStationsAsync(CancellationToken ct = default);
}
