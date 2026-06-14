using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexCommodityClient
{
    Task<IReadOnlyList<JsonDocument>> FetchAllCommoditiesAsync(CancellationToken ct = default);
}
