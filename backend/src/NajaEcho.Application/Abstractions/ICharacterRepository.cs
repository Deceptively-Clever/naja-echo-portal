using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Abstractions;

public interface ICharacterRepository
{
    Task<bool> HandleExistsAsync(string handle, CancellationToken ct = default);
    Task AddAsync(Character character, CancellationToken ct = default);
    Task<IReadOnlyList<Character>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
}
