namespace NajaEcho.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<(Guid Id, string DisplayName)>> GetAllAsync(CancellationToken ct);
}
