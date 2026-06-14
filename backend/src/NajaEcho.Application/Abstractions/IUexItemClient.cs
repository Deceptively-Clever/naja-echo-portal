using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexItemClient
{
    Task<IReadOnlyList<JsonDocument>> FetchItemsByCategoryAsync(int categoryId, CancellationToken ct = default);
}
