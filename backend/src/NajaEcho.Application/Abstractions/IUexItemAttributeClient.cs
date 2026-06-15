using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexItemAttributeClient
{
    Task<IReadOnlyList<JsonDocument>> FetchItemAttributesAsync(int uexItemId, CancellationToken ct = default);
}
