using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.Commodities;

namespace NajaEcho.Application.Features.Commodities.ImportCommodities;

public sealed class ImportCommoditiesHandler(
    ICommodityRepository repository,
    IUexCommodityClient commodityClient,
    IImportCoordinator coordinator,
    ILogger<ImportCommoditiesHandler> logger)
{
    public async Task<ImportCommoditiesResult> HandleAsync(ImportCommoditiesCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("ImportCommodities: acquiring lock");

        if (!coordinator.TryAcquire())
        {
            logger.LogWarning("ImportCommodities: already in progress");
            throw new ImportAlreadyInProgressException();
        }

        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            logger.LogInformation("ImportCommodities: fetching feed");
            IReadOnlyList<JsonDocument> records;
            try
            {
                records = await commodityClient.FetchAllCommoditiesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ImportCommodities: feed fetch failed");
                throw;
            }

            logger.LogInformation("ImportCommodities: source record count = {Count}", records.Count);

            if (records.Count == 0)
            {
                const string warning = "Feed returned zero records; no changes applied.";
                logger.LogWarning("ImportCommodities: {Warning}", warning);
                var zeroCompleted = DateTimeOffset.UtcNow;
                return new ImportCommoditiesResult(
                    0, 0, 0, 0, 0, 0,
                    startedAt, zeroCompleted,
                    (long)(zeroCompleted - startedAt).TotalMilliseconds,
                    warning);
            }

            int skipped = 0;
            var commodities = new List<Commodity>(records.Count);

            foreach (var doc in records)
            {
                var el = doc.RootElement;
                var id = GetInt(el, "id");
                var name = GetString(el, "name");

                if (id <= 0 || string.IsNullOrEmpty(name))
                {
                    skipped++;
                    continue;
                }

                commodities.Add(new Commodity
                {
                    UexId = id,
                    Uuid = GetString(el, "uuid"),
                    Name = name,
                    Code = GetString(el, "code"),
                    Slug = GetString(el, "slug"),
                    Kind = GetString(el, "kind"),
                    WeightScu = GetNullableDouble(el, "weight_scu"),
                    IdParent = GetNullableInt(el, "id_parent"),
                    IdItem = GetNullableInt(el, "id_item"),
                    Wiki = GetString(el, "wiki"),
                    IdsStarSystemsRaw = GetString(el, "ids_star_systems"),
                    IdsPlanetsRaw = GetString(el, "ids_planets"),
                    IdsMoonsRaw = GetString(el, "ids_moons"),
                    IdsPoiRaw = GetString(el, "ids_poi"),
                    IdsOrbitsRaw = GetString(el, "ids_orbits"),
                    IdsStarSystems = ParseIdList(el, "ids_star_systems"),
                    IdsPlanets = ParseIdList(el, "ids_planets"),
                    IdsMoons = ParseIdList(el, "ids_moons"),
                    IdsPoi = ParseIdList(el, "ids_poi"),
                    IdsOrbits = ParseIdList(el, "ids_orbits"),
                    IsAvailable = GetBool(el, "is_available"),
                    IsAvailableLive = GetBool(el, "is_available_live"),
                    IsVisible = GetBool(el, "is_visible"),
                    IsExtractable = GetBool(el, "is_extractable"),
                    IsMineral = GetBool(el, "is_mineral"),
                    IsRaw = GetBool(el, "is_raw"),
                    IsPure = GetBool(el, "is_pure"),
                    IsRefined = GetBool(el, "is_refined"),
                    IsRefinable = GetBool(el, "is_refinable"),
                    IsHarvestable = GetBool(el, "is_harvestable"),
                    IsBuyable = GetBool(el, "is_buyable"),
                    IsSellable = GetBool(el, "is_sellable"),
                    IsTemporary = GetBool(el, "is_temporary"),
                    IsIllegal = GetBool(el, "is_illegal"),
                    IsVolatileQt = GetBool(el, "is_volatile_qt"),
                    IsVolatileTime = GetBool(el, "is_volatile_time"),
                    IsInert = GetBool(el, "is_inert"),
                    IsExplosive = GetBool(el, "is_explosive"),
                    IsBuggy = GetBool(el, "is_buggy"),
                    IsFuel = GetBool(el, "is_fuel"),
                    SourceDateAdded = GetRawLong(el, "date_added"),
                    SourceDateModified = GetRawLong(el, "date_modified"),
                    SourceDateAddedUtc = GetDateTimeOffset(el, "date_added"),
                    SourceDateModifiedUtc = GetDateTimeOffset(el, "date_modified"),
                    RawData = doc,
                });
            }

            logger.LogInformation("ImportCommodities: mapped {Mapped} records, skipped {Skipped}", commodities.Count, skipped);

            var (ins, upd, res, sd) = await repository.BulkUpsertAsync(commodities, ct);

            var completedAt = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "ImportCommodities: completed — inserted={Ins} updated={Upd} restored={Res} softDeleted={SD} skipped={Skip}",
                ins, upd, res, sd, skipped);

            return new ImportCommoditiesResult(
                records.Count, skipped, ins, upd, res, sd,
                startedAt, completedAt,
                (long)(completedAt - startedAt).TotalMilliseconds);
        }
        catch (ImportAlreadyInProgressException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ImportCommodities: failed");
            throw;
        }
        finally
        {
            coordinator.Release();
        }
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static int GetInt(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : 0;

    private static int? GetNullableInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind != JsonValueKind.Number) return null;
        if (v.TryGetInt32(out var i)) return i;
        if (v.TryGetDouble(out var d)) return (int)d;
        return null;
    }

    private static double? GetNullableDouble(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
        return null;
    }

    private static bool GetBool(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return false;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => v.GetInt32() != 0,
            _ => false,
        };
    }

    private static long? GetRawLong(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var val)) return val;
        return null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var unix) && unix > 0)
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        if (v.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(v.GetString(), out var dto))
            return dto;
        return null;
    }

    private static int[] ParseIdList(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.String)
            return [];

        var raw = v.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var parts = raw.Split(',');
        var result = new List<int>(parts.Length);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out var id))
                result.Add(id);
        }
        return [.. result];
    }
}
