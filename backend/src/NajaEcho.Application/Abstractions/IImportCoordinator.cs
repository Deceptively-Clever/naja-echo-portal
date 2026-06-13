namespace NajaEcho.Application.Abstractions;

public interface IImportCoordinator
{
    /// <summary>Attempt to acquire the import lock. Returns false immediately if already held.</summary>
    bool TryAcquire();

    void Release();
}
