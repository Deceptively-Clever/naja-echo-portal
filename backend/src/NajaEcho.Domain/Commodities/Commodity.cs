using System.Text.Json;

namespace NajaEcho.Domain.Commodities;

public sealed class Commodity
{
    public Guid Id { get; set; }
    public int UexId { get; set; }
    public string? Uuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Slug { get; set; }
    public string? Kind { get; set; }
    public double? WeightScu { get; set; }
    public int? IdParent { get; set; }
    public int? IdItem { get; set; }
    public string? Wiki { get; set; }

    // Location identifiers — raw comma-separated strings
    public string? IdsStarSystemsRaw { get; set; }
    public string? IdsPlanetsRaw { get; set; }
    public string? IdsMoonsRaw { get; set; }
    public string? IdsPoiRaw { get; set; }
    public string? IdsOrbitsRaw { get; set; }

    // Location identifiers — parsed integer arrays
    public int[] IdsStarSystems { get; set; } = [];
    public int[] IdsPlanets { get; set; } = [];
    public int[] IdsMoons { get; set; } = [];
    public int[] IdsPoi { get; set; } = [];
    public int[] IdsOrbits { get; set; } = [];

    // Boolean flags (normalized from UEX integer fields)
    public bool IsAvailable { get; set; }
    public bool IsAvailableLive { get; set; }
    public bool IsVisible { get; set; }
    public bool IsExtractable { get; set; }
    public bool IsMineral { get; set; }
    public bool IsRaw { get; set; }
    public bool IsPure { get; set; }
    public bool IsRefined { get; set; }
    public bool IsRefinable { get; set; }
    public bool IsHarvestable { get; set; }
    public bool IsBuyable { get; set; }
    public bool IsSellable { get; set; }
    public bool IsTemporary { get; set; }
    public bool IsIllegal { get; set; }
    public bool IsVolatileQt { get; set; }
    public bool IsVolatileTime { get; set; }
    public bool IsInert { get; set; }
    public bool IsExplosive { get; set; }
    public bool IsBuggy { get; set; }
    public bool IsFuel { get; set; }

    // Timestamps — raw Unix seconds
    public long? SourceDateAdded { get; set; }
    public long? SourceDateModified { get; set; }

    // Timestamps — converted UTC
    public DateTimeOffset? SourceDateAddedUtc { get; set; }
    public DateTimeOffset? SourceDateModifiedUtc { get; set; }

    public CommodityStatus Status { get; set; }
    public JsonDocument RawData { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset ImportedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
}
