namespace NajaEcho.Domain.Warehouse;

public sealed class ShipComponentAttributes
{
    public Guid ItemId { get; set; }
    public string? Class { get; set; }
    public int? Size { get; set; }
    public string? Grade { get; set; }
    public DateTimeOffset AttributesFetchedAt { get; set; }
}
