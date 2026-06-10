using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NajaEcho.Api.Features.Auth.Contracts;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using AspNet.Security.OAuth.Discord;

namespace NajaEcho.Api.Features.Auth;

public static class AuthEndpoints
{
    private const string DiscordAvatarBase = "https://cdn.discordapp.com/avatars";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/discord/login", Login);
        app.MapPost("/api/auth/signout", SignOut).RequireAuthorization();
        app.MapGet("/api/auth/me", Me).RequireAuthorization();

        return app;
    }

    private static IResult Login(HttpContext ctx, IConfiguration config)
    {
        var frontendOrigin = config["Frontend:Origin"] ?? "";
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = $"{frontendOrigin}/dashboard" },
            [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> SignOut(HttpContext ctx)
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> Me(
        ClaimsPrincipal user,
        GetCurrentUserHandler handler,
        CancellationToken ct)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Results.Unauthorized();

        var dto = await handler.HandleAsync(new GetCurrentUserQuery(userId), ct);
        if (dto is null)
            return Results.Unauthorized();

        var avatarUrl = dto.AvatarRef is not null
            ? $"{DiscordAvatarBase}/{userId}/{dto.AvatarRef}.png"
            : null;

        return Results.Ok(new CurrentUserResponse(dto.Id, dto.DisplayName, avatarUrl));
    }
}
