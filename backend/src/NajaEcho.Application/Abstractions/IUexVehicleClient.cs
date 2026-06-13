using System.Text.Json;

namespace NajaEcho.Application.Abstractions;

public interface IUexVehicleClient
{
    /// <summary>Fetch all vehicles from the UEX feed. Returns the raw JSON records.</summary>
    Task<IReadOnlyList<JsonDocument>> FetchAllVehiclesAsync(CancellationToken ct = default);
}
