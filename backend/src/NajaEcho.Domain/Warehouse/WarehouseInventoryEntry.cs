namespace NajaEcho.Domain.Warehouse;

public sealed class WarehouseInventoryEntry
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Quality { get; set; } = 500;
    public Guid? LocationId { get; set; }
    public string? LocationType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
