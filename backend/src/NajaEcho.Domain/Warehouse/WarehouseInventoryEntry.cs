namespace NajaEcho.Domain.Warehouse;

public sealed class WarehouseInventoryEntry
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
