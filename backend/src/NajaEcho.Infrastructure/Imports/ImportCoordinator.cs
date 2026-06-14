using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Imports;

// Single-flight guard shared across all admin imports (ships, categories, items, commodities).
// It lives outside any single feature folder because every import feature depends on it.
// NOTE: the lock is an in-process SemaphoreSlim, so "one import at a time" only holds for a
// single API instance. If the API is ever scaled to multiple replicas, concurrent imports
// become possible and a same-uex_id race could surface as a unique-index violation. Replace
// this with a database-backed lock (e.g. a Postgres advisory lock) before scaling horizontally.
public sealed class ImportCoordinator : IImportCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryAcquire() => _semaphore.Wait(0);

    public void Release() => _semaphore.Release();
}
