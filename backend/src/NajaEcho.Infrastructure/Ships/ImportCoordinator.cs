using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Ships;

public sealed class ImportCoordinator : IImportCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryAcquire() => _semaphore.Wait(0);

    public void Release() => _semaphore.Release();
}
