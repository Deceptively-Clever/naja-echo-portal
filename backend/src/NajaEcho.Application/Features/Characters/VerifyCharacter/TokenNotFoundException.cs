namespace NajaEcho.Application.Features.Characters.VerifyCharacter;

public sealed class TokenNotFoundException() : Exception("Token not found on your RSI profile.") { }
