using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Abstractions;

public interface IPendingRegistrationRepository
{
    Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
    Task UpsertAsync(PendingCharacterRegistration pending, CancellationToken ct = default);
    Task RemoveByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
}
