namespace NajaEcho.Application.Features.Hangar.ImportHangar;

public sealed class ImportHangarHandler(Abstractions.IHangarRepository hangar)
{
    public async Task<ImportHangarResult> HandleAsync(ImportHangarCommand command, CancellationToken ct)
    {
        var identifiable = command.Items
            .Where(r => string.IsNullOrWhiteSpace(r.Unidentified))
            .ToList();

        var effectiveNames = identifiable
            .Select(r => !string.IsNullOrWhiteSpace(r.ShipName) ? r.ShipName.Trim() : r.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var catalog = effectiveNames.Count > 0
            ? await hangar.GetShipIdsByNamesAsync(effectiveNames, ct)
            : new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var matchedIds = identifiable
            .Select(r => !string.IsNullOrWhiteSpace(r.ShipName) ? r.ShipName.Trim() : r.Name.Trim())
            .Where(name => catalog.ContainsKey(name))
            .Select(name => catalog[name])
            .Distinct()
            .ToList();

        var unmatched = identifiable
            .Select(r => !string.IsNullOrWhiteSpace(r.ShipName) ? r.ShipName.Trim() : r.Name.Trim())
            .Where(name => !catalog.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        var skippedUnidentified = command.Items.Count - identifiable.Count;
        var unmatchedCount = unmatched.Count + skippedUnidentified;

        await hangar.ReplaceFromImportAsync(command.UserId, matchedIds, ct);

        return new ImportHangarResult(
            TotalRecords: command.Items.Count,
            ImportedShips: matchedIds.Count,
            UnmatchedRecords: unmatchedCount,
            UnmatchedShipNames: unmatched);
    }
}
