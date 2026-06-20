namespace NajaEcho.Application.Features.Characters.VerifyCharacter;

public sealed record VerifyCharacterCommand(Guid OwnerUserId, string Handle);
