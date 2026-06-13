using System.Text.Json;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Ships.GetShipById;

public sealed class GetShipByIdHandler(IShipRepository repository)
{
    public async Task<ShipDetail?> HandleAsync(GetShipByIdQuery query, CancellationToken ct = default)
    {
        var ship = await repository.GetByIdAsync(query.Id, ct);
        if (ship is null)
            return null;

        var fields = new Dictionary<string, object?>();
        foreach (var prop in ship.RawData.RootElement.EnumerateObject())
        {
            fields[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? (object?)l : prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => prop.Value.GetRawText(),
            };
        }

        return new ShipDetail(ship.Id, ship.Status, fields);
    }
}
