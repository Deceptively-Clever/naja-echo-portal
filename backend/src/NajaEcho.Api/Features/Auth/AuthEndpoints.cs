using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using NajaEcho.Api.Features.Auth.Contracts;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using AspNet.Security.OAuth.Discord;
using Serilog;

namespace NajaEcho.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/discord/login", Login).AllowAnonymous();
        app.MapPost("/api/auth/signout", (Delegate)SignOut).AllowAnonymous();
        app.MapGet("/api/auth/me", Me).AllowAnonymous();

        return app;
    }

    private static IResult Login(HttpContext ctx, IConfiguration config)
    {
        var frontendOrigin = config["Frontend:Origin"] ?? "";
        Log.Information("Discord login started");
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = $"{frontendOrigin}/dashboard" },
            [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> SignOut(HttpContext ctx)
    {
        await ctx.SignOutAsync(IdentityConstants.ApplicationScheme);
        Log.Information("Sign-out completed");
        return Results.NoContent();
    }

    private static async Task<IResult> Me(
        ClaimsPrincipal user,
        GetCurrentUserHandler handler,
        CancellationToken ct)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Results.Ok(new AnonymousSessionResponse());

        var localUser = await handler.HandleAsync(new GetCurrentUserQuery(userId), ct);
        if (localUser is null)
            return Results.Ok(new AnonymousSessionResponse());

        return Results.Ok(new AuthenticatedSessionResponse(
            new CurrentUserResponse(localUser.Id, localUser.DisplayName, localUser.DiscordUsername)));
    }
}
