using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Items;

public sealed class UexItemAttributeClient(HttpClient http, ILogger<UexItemAttributeClient> logger) : IUexItemAttributeClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchItemAttributesAsync(int uexItemId, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX item attributes for uexItemId={UexItemId}", uexItemId);

        using var response = await http.GetAsync($"items_attributes?id_item={uexItemId}", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"UEX item_attributes response for item {uexItemId} missing 'data' array.");
        }

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
        {
            results.Add(JsonDocument.Parse(element.GetRawText()));
        }

        logger.LogInformation("UEX item_attributes returned {Count} records for uexItemId={UexItemId}", results.Count, uexItemId);
        return results;
    }
}
