using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.GetCharacters;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Features.Characters.VerifyCharacter;

public sealed class VerifyCharacterHandler(
    IPendingRegistrationRepository pendingRepository,
    ICharacterRepository characterRepository,
    IRsiCitizenClient rsiClient,
    IClock clock,
    ILogger<VerifyCharacterHandler> logger)
{
    public async Task<CharacterDto> HandleAsync(VerifyCharacterCommand command, CancellationToken ct)
    {
        var handle = command.Handle.Trim();

        var pending = await pendingRepository.GetByOwnerAsync(command.OwnerUserId, ct);
        if (pending is null || pending.IsExpired(clock.UtcNow))
            throw new TokenExpiredException();

        var handleExists = await characterRepository.HandleExistsAsync(handle, ct);
        if (handleExists)
            throw new HandleAlreadyClaimedException();

        logger.LogInformation("VerifyCharacter owner={OwnerId} handle={Handle} fetching RSI page",
            command.OwnerUserId, handle);

        var result = await rsiClient.FetchCitizenAsync(handle, ct);

        string content;
        string? displayName;

        switch (result)
        {
            case RsiCitizenPage page:
                content = page.Content;
                displayName = page.DisplayName;
                break;
            case RsiProfileNotFound:
                throw new RsiProfileNotFoundException();
            default:
                throw new RsiUnreachableException();
        }

        if (!content.Contains(pending.Token, StringComparison.Ordinal))
            throw new TokenNotFoundException();

        var name = string.IsNullOrWhiteSpace(displayName) ? handle : displayName.Trim();
        if (name.Length > 100) name = name[..100];

        var character = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.OwnerUserId,
            Name = name,
            Handle = handle,
            CreatedAt = clock.UtcNow,
        };

        await characterRepository.AddAsync(character, ct);
        await pendingRepository.RemoveByOwnerAsync(command.OwnerUserId, ct);

        logger.LogInformation("VerifyCharacter owner={OwnerId} handle={Handle} character created id={CharacterId} name={Name}",
            command.OwnerUserId, handle, character.Id, character.Name);

        return new CharacterDto(character.Id, character.Name, character.Handle, character.CreatedAt);
    }
}
