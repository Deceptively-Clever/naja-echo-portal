using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Commodities;

public sealed class UexCommodityClient(HttpClient http, ILogger<UexCommodityClient> logger) : IUexCommodityClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchAllCommoditiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX commodity feed");

        using var response = await http.GetAsync("commodities", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("UEX commodity feed response missing 'data' array.");

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
            results.Add(JsonDocument.Parse(element.GetRawText()));

        logger.LogInformation("UEX commodity feed returned {Count} records", results.Count);
        return results;
    }
}
