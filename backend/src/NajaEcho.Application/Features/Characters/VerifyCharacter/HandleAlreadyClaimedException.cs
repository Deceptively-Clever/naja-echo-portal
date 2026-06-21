namespace NajaEcho.Application.Features.Characters.VerifyCharacter;

public sealed class HandleAlreadyClaimedException() : Exception("This handle is already claimed.") { }
