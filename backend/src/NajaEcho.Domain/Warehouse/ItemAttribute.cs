namespace NajaEcho.Domain.Warehouse;

public sealed class ItemAttribute
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public int? UexAttributeId { get; set; }
    public int UexItemId { get; set; }
    public int? UexCategoryId { get; set; }
    public int UexCategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? SourceDateAdded { get; set; }
    public DateTimeOffset? SourceDateModified { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
