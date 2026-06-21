namespace NajaEcho.Application.Features.Admin.Users.AddCharacterForUser;

public sealed class CharacterNameUnavailableException()
    : Exception("Character name could not be retrieved — the handle may be valid but the RSI page returned no name.") { }
