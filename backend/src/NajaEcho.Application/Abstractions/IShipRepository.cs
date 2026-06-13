using NajaEcho.Domain.Ships;

namespace NajaEcho.Application.Abstractions;

public interface IShipRepository
{
    Task<(IReadOnlyList<Ship> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Ship?> GetByUexIdAsync(int uexId, CancellationToken ct = default);

    /// <summary>Upsert ships transactionally: insert/update/reactivate incoming, soft-delete absent Active ones.</summary>
    Task<(int Added, int Updated, int Reactivated, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Ship> incomingShips, CancellationToken ct = default);

    Task<IReadOnlyList<int>> GetAllActiveUexIdsAsync(CancellationToken ct = default);
}
