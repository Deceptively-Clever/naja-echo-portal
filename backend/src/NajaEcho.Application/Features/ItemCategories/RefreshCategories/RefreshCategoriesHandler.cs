using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Ships.ImportShips;
using NajaEcho.Domain.ItemCategories;

namespace NajaEcho.Application.Features.ItemCategories.RefreshCategories;

public sealed class RefreshCategoriesHandler(
    IItemCategoryRepository repository,
    IUexCategoryClient categoryClient,
    IImportCoordinator coordinator,
    ILogger<RefreshCategoriesHandler> logger)
{
    public async Task<RefreshCategoriesResult> HandleAsync(RefreshCategoriesCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Refresh categories: acquiring lock");

        if (!coordinator.TryAcquire())
        {
            logger.LogWarning("Refresh categories: already in progress");
            throw new ImportAlreadyInProgressException();
        }

        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            logger.LogInformation("Refresh categories: fetching feed");
            IReadOnlyList<JsonDocument> records;
            try
            {
                records = await categoryClient.FetchAllCategoriesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refresh categories: feed fetch failed");
                throw;
            }

            logger.LogInformation("Refresh categories: source record count = {Count}", records.Count);

            var categories = records.Select(MapToCategory).ToList();
            var (inserted, updated, unchanged) = await repository.BulkUpsertAsync(categories, ct);

            var completedAt = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "Refresh categories: completed — inserted={Inserted} updated={Updated} unchanged={Unchanged}",
                inserted, updated, unchanged);

            return new RefreshCategoriesResult(records.Count, inserted, updated, unchanged, 0, startedAt, completedAt);
        }
        catch (ImportAlreadyInProgressException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refresh categories: failed");
            throw;
        }
        finally
        {
            coordinator.Release();
        }
    }

    private static ItemCategory MapToCategory(JsonDocument doc)
    {
        var el = doc.RootElement;

        return new ItemCategory
        {
            UexId = el.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            Type = el.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "",
            Section = el.TryGetProperty("section", out var section) ? section.GetString() : null,
            Name = el.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            IsGameRelated = GetBool(el, "is_game_related"),
            IsMining = GetBool(el, "is_mining"),
            SourceDateAdded = GetDateTimeOffset(el, "date_added"),
            SourceDateModified = GetDateTimeOffset(el, "date_modified"),
            RawData = doc,
        };
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

    private static DateTimeOffset? GetDateTimeOffset(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var unix))
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        if (v.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(
                v.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            return dto;
        return null;
    }
}
