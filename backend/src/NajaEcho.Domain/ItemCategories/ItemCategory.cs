using System.Text.Json;

namespace NajaEcho.Domain.ItemCategories;

public sealed class ItemCategory
{
    public Guid Id { get; set; }
    public int UexId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsGameRelated { get; set; }
    public bool IsMining { get; set; }
    public DateTimeOffset? SourceDateAdded { get; set; }
    public DateTimeOffset? SourceDateModified { get; set; }
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset ImportedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
