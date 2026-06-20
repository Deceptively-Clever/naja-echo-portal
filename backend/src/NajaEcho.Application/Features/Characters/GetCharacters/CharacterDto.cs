namespace NajaEcho.Application.Features.Characters.GetCharacters;

public sealed record CharacterDto(Guid Id, string Name, string Handle, DateTimeOffset CreatedAt);
