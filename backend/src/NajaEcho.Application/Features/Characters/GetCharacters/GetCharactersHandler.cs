using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Characters.GetCharacters;

public sealed class GetCharactersHandler(
    ICharacterRepository repository,
    ILogger<GetCharactersHandler> logger)
{
    public async Task<IReadOnlyList<CharacterDto>> HandleAsync(GetCharactersQuery query, CancellationToken ct)
    {
        var characters = await repository.GetByOwnerAsync(query.OwnerUserId, ct);

        logger.LogInformation("GetCharacters owner={OwnerId} returned {Count} characters",
            query.OwnerUserId, characters.Count);

        return characters.Select(c => new CharacterDto(c.Id, c.Name, c.Handle, c.CreatedAt)).ToList();
    }
}
