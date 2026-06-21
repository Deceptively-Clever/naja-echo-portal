using System.Text.Json;

namespace NajaEcho.Domain.Locations;

public sealed class SpaceStation
{
    public Guid Id { get; set; }
    public int UexId { get; set; }
    public Guid StarSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsDecommissioned { get; set; }
    public bool IsLandable { get; set; }
    public bool HasRefinery { get; set; }
    public bool HasTradeTerminal { get; set; }
    public string Status { get; set; } = CatalogStatus.Active;
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset ImportedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
    public StarSystem? StarSystem { get; set; }
}
