namespace NajaEcho.Api.Features.Characters.Contracts;

public sealed record StartRegistrationResponse(string Token, DateTimeOffset ExpiresAt);
public sealed record PendingRegistrationResponse(string Token, DateTimeOffset ExpiresAt);
public sealed record VerifyCharacterRequest(string Handle);
public sealed record CharacterResponse(Guid Id, string Name, string Handle, DateTimeOffset CreatedAt);
public sealed record CharacterListResponse(IReadOnlyList<CharacterResponse> Characters);
