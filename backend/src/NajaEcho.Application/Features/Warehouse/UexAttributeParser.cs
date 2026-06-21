using System.Text.Json;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Application.Features.Warehouse;

internal static class UexAttributeParser
{
    internal static IReadOnlyList<ItemAttribute> Parse(
        Guid itemId, int uexItemId, IReadOnlyList<JsonDocument> docs, DateTimeOffset fetchedAt)
    {
        var attrs = new List<ItemAttribute>(docs.Count);
        foreach (var doc in docs)
        {
            try
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("id_category_attribute", out var idCatAttrEl) ||
                    !idCatAttrEl.TryGetInt32(out var catAttrId))
                {
                    continue;
                }

                root.TryGetProperty("value", out var valueEl);
                root.TryGetProperty("unit", out var unitEl);
                root.TryGetProperty("id", out var idEl);
                root.TryGetProperty("id_category", out var idCatEl);
                root.TryGetProperty("date_added", out var dateAddedEl);
                root.TryGetProperty("date_modified", out var dateModifiedEl);
                root.TryGetProperty("attribute_name", out var attrName);

                var attr = new ItemAttribute
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    UexItemId = uexItemId,
                    UexCategoryAttributeId = catAttrId,
                    AttributeName = attrName.ValueKind == JsonValueKind.String ? attrName.GetString() ?? string.Empty : string.Empty,
                    Value = valueEl.ValueKind == JsonValueKind.String ? valueEl.GetString() : null,
                    Unit = unitEl.ValueKind == JsonValueKind.String ? unitEl.GetString() : null,
                    FetchedAt = fetchedAt,
                };

                if (idEl.TryGetInt32(out var uexAttrId))
                {
                    attr.UexAttributeId = uexAttrId;
                }

                if (idCatEl.TryGetInt32(out var uexCatId))
                {
                    attr.UexCategoryId = uexCatId;
                }

                if (dateAddedEl.ValueKind == JsonValueKind.Number && dateAddedEl.TryGetInt64(out var addedTs))
                {
                    attr.SourceDateAdded = DateTimeOffset.FromUnixTimeSeconds(addedTs);
                }

                if (dateModifiedEl.ValueKind == JsonValueKind.Number && dateModifiedEl.TryGetInt64(out var modifiedTs))
                {
                    attr.SourceDateModified = DateTimeOffset.FromUnixTimeSeconds(modifiedTs);
                }

                attrs.Add(attr);
            }
            catch
            {
                // skip malformed attribute entries
            }
        }
        return attrs;
    }
}
