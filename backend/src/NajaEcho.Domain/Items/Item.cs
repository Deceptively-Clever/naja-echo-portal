using System.Text.Json;

namespace NajaEcho.Domain.Items;

public sealed class Item
{
    public Guid Id { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public int UexId { get; set; }
    public int? IdParent { get; set; }
    public int IdCategory { get; set; }
    public int? IdCompany { get; set; }
    public int? IdVehicle { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? Category { get; set; }
    public string? CompanyName { get; set; }
    public string? VehicleName { get; set; }
    public string? Slug { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Color2 { get; set; }
    public string? UrlStore { get; set; }
    public string? Wiki { get; set; }
    public string? Quality { get; set; }
    public bool IsExclusivePledge { get; set; }
    public bool IsExclusiveSubscriber { get; set; }
    public bool IsExclusiveConcierge { get; set; }
    public bool IsCommodity { get; set; }
    public bool IsHarvestable { get; set; }
    public string? Notification { get; set; }
    public string? GameVersion { get; set; }
    public DateTimeOffset? SourceDateAdded { get; set; }
    public DateTimeOffset? SourceDateModified { get; set; }
    public ItemStatus Status { get; set; }
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset ImportedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
}
