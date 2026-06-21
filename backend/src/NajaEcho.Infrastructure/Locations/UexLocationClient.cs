using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Locations;

public sealed class UexLocationClient(HttpClient httpClient, ILogger<UexLocationClient> logger) : IUexLocationClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchAllStarSystemsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX star systems feed");

        using var response = await httpClient.GetAsync("star_systems", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("UEX feed response missing 'data' array.");

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
        {
            results.Add(JsonDocument.Parse(element.GetRawText()));
        }

        logger.LogInformation("UEX feed returned {Count} star system records", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<JsonDocument>> FetchAllSpaceStationsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX space stations feed");

        using var response = await httpClient.GetAsync("space_stations", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var root = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!root.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("UEX feed response missing 'data' array.");

        var results = new List<JsonDocument>(dataEl.GetArrayLength());
        foreach (var element in dataEl.EnumerateArray())
        {
            results.Add(JsonDocument.Parse(element.GetRawText()));
        }

        logger.LogInformation("UEX feed returned {Count} space station records", results.Count);
        return results;
    }
}
