using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.ItemCategories;

public sealed class UexCategoryClient(HttpClient http, ILogger<UexCategoryClient> logger) : IUexCategoryClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchAllCategoriesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX categories feed");

        using var response = await http.GetAsync("categories", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("UEX categories response missing 'data' array.");

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
            results.Add(JsonDocument.Parse(element.GetRawText()));

        logger.LogInformation("UEX categories feed returned {Count} records", results.Count);
        return results;
    }
}
