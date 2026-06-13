using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Ships;

public sealed class UexVehicleClient(HttpClient http, ILogger<UexVehicleClient> logger) : IUexVehicleClient
{
    public async Task<IReadOnlyList<JsonDocument>> FetchAllVehiclesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching UEX vehicle feed");

        using var response = await http.GetAsync("vehicles", ct);
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

        logger.LogInformation("UEX feed returned {Count} vehicle records", results.Count);
        return results;
    }
}
