using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Admin.Users.GetUsers;
using NajaEcho.Application.Features.Characters.VerifyCharacter;
using NajaEcho.Domain.Characters;

namespace NajaEcho.Application.Features.Admin.Users.AddCharacterForUser;

public sealed class AddCharacterForUserHandler(
    IUserRepository userRepository,
    ICharacterRepository characterRepository,
    IRsiCitizenClient rsiClient,
    IClock clock,
    ILogger<AddCharacterForUserHandler> logger)
{
    public async Task<AdminUserCharacterDto> HandleAsync(AddCharacterForUserCommand command, CancellationToken ct)
    {
        var handle = command.Handle.Trim();

        if (string.IsNullOrWhiteSpace(handle))
        {
            throw new ArgumentException("Handle must not be blank.", nameof(command));
        }

        var userExists = await userRepository.ExistsAsync(command.TargetUserId, ct);
        if (!userExists)
        {
            throw new UserNotFoundException(command.TargetUserId);
        }

        var handleExists = await characterRepository.HandleExistsAsync(handle, ct);
        if (handleExists)
        {
            throw new HandleAlreadyClaimedException();
        }

        logger.LogInformation(
            "AddCharacterForUser targetUserId={TargetUserId} handle={Handle} fetching RSI page",
            command.TargetUserId, handle);

        var result = await rsiClient.FetchCitizenAsync(handle, ct);

        string? displayName;
        switch (result)
        {
            case RsiCitizenPage page:
                displayName = page.DisplayName;
                break;
            case RsiProfileNotFound:
                throw new RsiProfileNotFoundException();
            default:
                throw new RsiUnreachableException();
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new CharacterNameUnavailableException();
        }

        var name = displayName.Trim();
        if (name.Length > 100)
        {
            name = name[..100];
        }

        var character = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.TargetUserId,
            Name = name,
            Handle = handle,
            CreatedAt = clock.UtcNow,
        };

        await characterRepository.AddAsync(character, ct);

        logger.LogInformation(
            "AddCharacterForUser targetUserId={TargetUserId} handle={Handle} outcome=success characterId={CharacterId} name={Name}",
            command.TargetUserId, handle, character.Id, character.Name);

        return new AdminUserCharacterDto(character.Id, character.Name, character.Handle);
    }
}
