using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Characters.GetRegistration;

public sealed class GetRegistrationHandler(
    IPendingRegistrationRepository repository,
    IClock clock,
    ILogger<GetRegistrationHandler> logger)
{
    public async Task<PendingRegistrationDto?> HandleAsync(GetRegistrationQuery query, CancellationToken ct)
    {
        var pending = await repository.GetByOwnerAsync(query.OwnerUserId, ct);

        if (pending is null || pending.IsExpired(clock.UtcNow))
        {
            logger.LogInformation("GetRegistration owner={OwnerId} no active pending registration", query.OwnerUserId);
            return null;
        }

        logger.LogInformation("GetRegistration owner={OwnerId} returning active token (expires {ExpiresAt})",
            query.OwnerUserId, pending.ExpiresAt);

        return new PendingRegistrationDto(pending.Token, pending.ExpiresAt);
    }
}
