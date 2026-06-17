namespace NajaEcho.Domain.Warehouse;

public sealed class WarehouseMaterialEntry
{
    public Guid Id { get; set; }
    public Guid CommodityId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int Quality { get; set; } = 500;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
