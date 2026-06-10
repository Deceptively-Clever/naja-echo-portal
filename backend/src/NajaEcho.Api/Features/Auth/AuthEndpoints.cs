using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NajaEcho.Api.Features.Auth.Contracts;
using NajaEcho.Application.Features.Auth.GetCurrentUser;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Domain.Users;
using AspNet.Security.OAuth.Discord;

namespace NajaEcho.Api.Features.Auth;

public static class AuthEndpoints
{
    private const string DiscordAvatarBase = "https://cdn.discordapp.com/avatars";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/discord/login", Login);
        app.MapGet("/api/auth/discord/callback", Callback);
        app.MapPost("/api/auth/signout", SignOut).RequireAuthorization();
        app.MapGet("/api/auth/me", Me).RequireAuthorization();

        return app;
    }

    private static IResult Login(HttpContext ctx)
    {
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/dashboard" },
            [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> Callback(
        HttpContext ctx,
        SignInWithDiscordHandler handler,
        CancellationToken ct)
    {
        // OAuth errors arrive as query params or via the Remote failure event
        if (ctx.Request.Query.ContainsKey("error"))
        {
            var reason = ctx.Request.Query["error"].ToString();
            return Results.Redirect($"/auth/error?reason={Uri.EscapeDataString(reason)}");
        }

        var result = await ctx.AuthenticateAsync(DiscordAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Results.Redirect("/auth/error?reason=auth_failed");

        var claims = result.Principal?.Claims.ToList() ?? [];

        var discordProfile = ExtractProfile(claims, result.Properties);
        if (discordProfile is null)
            return Results.Redirect("/auth/error?reason=profile_unavailable");

        var signInResult = await handler.HandleAsync(new SignInWithDiscordCommand(discordProfile), ct);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, signInResult.UserId.ToString()),
            new Claim(ClaimTypes.Name, signInResult.DisplayName),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });

        return Results.Redirect("/dashboard");
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

    private static DiscordProfile? ExtractProfile(List<Claim> claims, AuthenticationProperties? props)
    {
        var id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        if (id is null || username is null) return null;

        return new DiscordProfile
        {
            Id = id,
            Username = username,
            GlobalName = claims.FirstOrDefault(c => c.Type == "urn:discord:user:global_name")?.Value,
            Avatar = claims.FirstOrDefault(c => c.Type == "urn:discord:user:avatar")?.Value,
            Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            Verified = claims.FirstOrDefault(c => c.Type == "urn:discord:user:verified")?.Value == "true",
        };
    }
}
