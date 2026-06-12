namespace NajaEcho.Api.Features.Auth.Contracts;

public sealed record AnonymousSessionResponse
{
    public bool Authenticated => false;
}

public sealed record AuthenticatedSessionResponse(CurrentUserResponse User)
{
    public bool Authenticated => true;
}
