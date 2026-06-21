using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Ships.ImportShips;

namespace NajaEcho.Application.Features.Items.ImportItems;

public sealed class ImportItemsHandler(
    IItemCategoryRepository categoryRepository,
    IItemRepository itemRepository,
    IUexItemClient itemClient,
    IImportCoordinator coordinator,
    ILogger<ImportItemsHandler> logger)
{
    public async Task<ImportItemsResult> HandleAsync(ImportItemsCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("ImportItems: acquiring lock (categoryUexId={Cat})", command.CategoryUexId?.ToString() ?? "all");

        if (!coordinator.TryAcquire())
        {
            logger.LogWarning("ImportItems: already in progress");
            throw new ImportAlreadyInProgressException();
        }

        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            IReadOnlyList<Domain.ItemCategories.ItemCategory> categories;

            if (command.CategoryUexId.HasValue)
            {
                var eligible = await categoryRepository.GetEligibleAsync(ct);
                var single = eligible.FirstOrDefault(c => c.UexId == command.CategoryUexId.Value);
                if (single is null)
                {
                    logger.LogWarning("ImportItems: category {Id} not found or not eligible", command.CategoryUexId.Value);
                    var completedAt = DateTimeOffset.UtcNow;
                    return new ImportItemsResult(
                        ImportStatus.Failed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        startedAt, completedAt,
                        [new CategoryImportError(command.CategoryUexId.Value, null, "Category not found or not eligible for import.")]);
                }
                categories = [single];
            }
            else
            {
                categories = await categoryRepository.GetEligibleAsync(ct);
            }

            if (categories.Count == 0)
            {
                logger.LogInformation("ImportItems: no eligible categories");
                return new ImportItemsResult(
                    ImportStatus.Success, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    startedAt, DateTimeOffset.UtcNow, []);
            }

            int totalFetched = 0, totalInserted = 0, totalUpdated = 0, totalUnchanged = 0;
            int totalSoftDeleted = 0, totalFailed = 0;
            int categoriesSucceeded = 0, categoriesFailed = 0;
            var errors = new List<CategoryImportError>();

            foreach (var category in categories)
            {
                try
                {
                    logger.LogInformation("ImportItems: fetching items for category {Id} ({Name})", category.UexId, category.Name);
                    var rawItems = await itemClient.FetchItemsByCategoryAsync(category.UexId, ct);
                    totalFetched += rawItems.Count;

                    var items = MapToItems(rawItems, category.UexId);

                    var (inserted, updated, unchanged, softDeleted, restored) =
                        await itemRepository.BulkUpsertForCategoryAsync(category.UexId, items, ct);

                    totalInserted += inserted;
                    totalUpdated += updated + restored;
                    totalUnchanged += unchanged;
                    totalSoftDeleted += softDeleted;
                    categoriesSucceeded++;

                    logger.LogInformation(
                        "ImportItems: category {Id} done — inserted={Ins} updated={Upd} unchanged={Unch} softDeleted={SD}",
                        category.UexId, inserted, updated + restored, unchanged, softDeleted);
                }
                catch (Exception ex)
                {
                    categoriesFailed++;
                    totalFailed++;
                    errors.Add(new CategoryImportError(category.UexId, category.Name, ex.Message));
                    logger.LogError(ex, "ImportItems: category {Id} ({Name}) failed", category.UexId, category.Name);
                }
            }

            var status = categoriesFailed == 0
                ? ImportStatus.Success
                : categoriesSucceeded > 0
                    ? ImportStatus.CompletedWithErrors
                    : ImportStatus.Failed;

            var completedAt2 = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "ImportItems: completed — status={Status} cats={Total}/{Succ} items={Ins}+{Upd}+{Unch} SD={SD}",
                status, categories.Count, categoriesSucceeded, totalInserted, totalUpdated, totalUnchanged, totalSoftDeleted);

            return new ImportItemsResult(
                status,
                categories.Count, categoriesSucceeded, categoriesFailed,
                totalFetched, totalInserted, totalUpdated, totalUnchanged,
                0, totalSoftDeleted, totalFailed,
                startedAt, completedAt2, errors);
        }
        catch (ImportAlreadyInProgressException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ImportItems: unhandled failure");
            throw;
        }
        finally
        {
            coordinator.Release();
        }
    }

    private static List<Domain.Items.Item> MapToItems(
        IReadOnlyList<System.Text.Json.JsonDocument> records,
        int categoryUexId)
    {
        var items = new List<Domain.Items.Item>(records.Count);

        foreach (var doc in records)
        {
            var el = doc.RootElement;

            items.Add(new Domain.Items.Item
            {
                Uuid = GetString(el, "uuid") ?? string.Empty,
                UexId = GetInt(el, "id"),
                IdParent = GetNullableInt(el, "id_parent"),
                IdCategory = categoryUexId,
                IdCompany = GetNullableInt(el, "id_company"),
                IdVehicle = GetNullableInt(el, "id_vehicle"),
                Name = GetString(el, "name") ?? string.Empty,
                Section = GetString(el, "section"),
                Category = GetString(el, "category"),
                CompanyName = GetString(el, "company_name"),
                VehicleName = GetString(el, "vehicle_name"),
                Slug = GetString(el, "slug"),
                Size = GetNullableIntAsString(el, "size"),
                Color = GetString(el, "color"),
                Color2 = GetString(el, "color2"),
                UrlStore = GetString(el, "url_store"),
                Wiki = GetString(el, "wiki"),
                Quality = GetNullableIntAsString(el, "quality"),
                IsExclusivePledge = GetBool(el, "is_exclusive_pledge"),
                IsExclusiveSubscriber = GetBool(el, "is_exclusive_subscriber"),
                IsExclusiveConcierge = GetBool(el, "is_exclusive_concierge"),
                IsCommodity = GetBool(el, "is_commodity"),
                IsHarvestable = GetBool(el, "is_harvestable"),
                Notification = GetString(el, "notification"),
                GameVersion = GetString(el, "game_version"),
                SourceDateAdded = GetDateTimeOffset(el, "date_added"),
                SourceDateModified = GetDateTimeOffset(el, "date_modified"),
                RawData = doc,
            });
        }

        return items;
    }

    private static string? GetString(System.Text.Json.JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
            ? v.GetString()
            : null;

    private static int GetInt(System.Text.Json.JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
            ? v.GetInt32()
            : 0;

    private static int? GetNullableInt(System.Text.Json.JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
        {
            return null;
        }

        if (v.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return v.GetInt32();
        }

        return null;
    }

    private static string? GetNullableIntAsString(System.Text.Json.JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
        {
            return null;
        }

        if (v.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return v.GetInt32().ToString();
        }

        if (v.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return v.GetString();
        }

        return null;
    }

    private static bool GetBool(System.Text.Json.JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
        {
            return false;
        }

        return v.ValueKind switch
        {
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Number => v.GetInt32() != 0,
            _ => false,
        };
    }

    private static DateTimeOffset? GetDateTimeOffset(System.Text.Json.JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
        {
            return null;
        }

        if (v.ValueKind == System.Text.Json.JsonValueKind.Number && v.TryGetInt64(out var unix))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        if (v.ValueKind == System.Text.Json.JsonValueKind.String && DateTimeOffset.TryParse(
                v.GetString(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var dto))
        {
            return dto;
        }

        return null;
    }
}
