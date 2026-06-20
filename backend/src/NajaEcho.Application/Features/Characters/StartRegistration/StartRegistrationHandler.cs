using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Features.Characters.StartRegistration;

public sealed class StartRegistrationHandler(
    IPendingRegistrationRepository repository,
    IClock clock,
    ILogger<StartRegistrationHandler> logger)
{
    public async Task<PendingRegistrationDto> HandleAsync(StartRegistrationCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var existing = await repository.GetByOwnerAsync(command.OwnerUserId, ct);

        if (existing is not null && !existing.IsExpired(now))
        {
            logger.LogInformation("StartRegistration owner={OwnerId} returning existing token (expires {ExpiresAt})",
                command.OwnerUserId, existing.ExpiresAt);
            return new PendingRegistrationDto(existing.Token, existing.ExpiresAt);
        }

        var pending = PendingCharacterRegistration.Create(command.OwnerUserId, now);
        await repository.UpsertAsync(pending, ct);

        logger.LogInformation("StartRegistration owner={OwnerId} created fresh token (expires {ExpiresAt})",
            command.OwnerUserId, pending.ExpiresAt);

        return new PendingRegistrationDto(pending.Token, pending.ExpiresAt);
    }
}
