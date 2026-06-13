using System.Text.Json;

namespace NajaEcho.Domain.Ships;

public sealed class Ship
{
    public Guid Id { get; set; }
    public int UexId { get; set; }
    public string? Uuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameFull { get; set; }
    public string? CompanyName { get; set; }
    public ShipStatus Status { get; set; }
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset ImportedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
}
