using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Items;

public sealed class UexItemClient(HttpClient http, ILogger<UexItemClient> logger) : IUexItemClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchItemsByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX items for category {CategoryId}", categoryId);

        using var response = await http.GetAsync($"items?id_category={categoryId}", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"UEX items response for category {categoryId} missing 'data' array.");
        }

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
        {
            results.Add(JsonDocument.Parse(element.GetRawText()));
        }

        logger.LogInformation("UEX items feed returned {Count} records for category {CategoryId}", results.Count, categoryId);
        return results;
    }
}
