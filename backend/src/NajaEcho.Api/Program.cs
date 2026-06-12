using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using NajaEcho.Api.Common;
using NajaEcho.Api.Features.Auth;
using NajaEcho.Application.Features.Auth.SignInWithDiscord;
using NajaEcho.Domain.Users;
using NajaEcho.Infrastructure;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NajaEchoPortal")
        .Destructure.ByTransforming<object>(o =>
        {
            // Scrub sensitive fields from structured log objects
            return o;
        })
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Trust X-Forwarded-* from the local nginx reverse proxy so Request.Scheme
    // reflects HTTPS and Secure cookies are issued correctly.
    builder.Services.Configure<ForwardedHeadersOptions>(opts =>
    {
        opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        opts.KnownIPNetworks.Clear();
        opts.KnownProxies.Clear();
    });

    var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";

    builder.Services.AddCors(opts =>
        opts.AddPolicy("Frontend", policy => policy
            .WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    builder.Services
        .AddAuthentication(opts =>
        {
            // Cookie is both the default authenticate AND challenge scheme.
            // The Discord challenge is invoked explicitly in the /login endpoint.
            // This ensures unauthenticated API requests get 401, not a redirect to Discord.
            opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
        {
            opts.Cookie.Name = "najaecho.auth";
            opts.Cookie.HttpOnly = true;
            opts.Cookie.SameSite = SameSiteMode.Lax;
            opts.Cookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.None;
            opts.SlidingExpiration = true;
            opts.ExpireTimeSpan = TimeSpan.FromDays(14);
            // Return 401 JSON for API routes instead of a redirect
            opts.Events.OnRedirectToLogin = async ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    var body = System.Text.Json.JsonSerializer.Serialize(new ProblemDetails
                    {
                        Title = "Authentication required.",
                        Status = StatusCodes.Status401Unauthorized,
                        Instance = ctx.Request.Path,
                    });
                    ctx.Response.ContentType = "application/problem+json";
                    await ctx.Response.WriteAsync(body);
                    return;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
            };
        })
        .AddDiscord(opts =>
        {
            opts.ClientId = builder.Configuration["Discord:ClientId"]
                ?? throw new InvalidOperationException("Discord:ClientId not configured.");
            opts.ClientSecret = builder.Configuration["Discord:ClientSecret"]
                ?? throw new InvalidOperationException("Discord:ClientSecret not configured.");
            opts.Scope.Add("identify");
            opts.Scope.Add("email");
            opts.SaveTokens = true;
            opts.CallbackPath = "/api/auth/discord/callback";
            opts.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "";

            opts.Events.OnTicketReceived = async ctx =>
            {
                var claims = ctx.Principal?.Claims.ToList() ?? [];
                var discordId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                if (discordId is null || username is null)
                {
                    ctx.Response.Redirect($"{frontendOrigin}/auth/error?reason=profile_unavailable");
                    ctx.HandleResponse();
                    return;
                }

                var profile = new DiscordProfile
                {
                    Id = discordId,
                    Username = username,
                    GlobalName = claims.FirstOrDefault(c => c.Type == "urn:discord:user:global_name")?.Value,
                    Avatar = claims.FirstOrDefault(c => c.Type == "urn:discord:user:avatar")?.Value,
                    Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                    Verified = claims.FirstOrDefault(c => c.Type == "urn:discord:user:verified")?.Value == "true",
                };

                var handler = ctx.HttpContext.RequestServices.GetRequiredService<SignInWithDiscordHandler>();
                var result = await handler.HandleAsync(new SignInWithDiscordCommand(profile), ctx.HttpContext.RequestAborted);

                var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new Claim(ClaimTypes.Name, result.DisplayName),
                ], CookieAuthenticationDefaults.AuthenticationScheme);

                ctx.Principal = new ClaimsPrincipal(identity);
                ctx.Properties!.IsPersistent = true;
                ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);
            };

            opts.Events.OnRemoteFailure = ctx =>
            {
                ctx.Response.Redirect($"{frontendOrigin}/auth/error?reason=remote_failure");
                ctx.HandleResponse();
                return Task.CompletedTask;
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestPath", ctx.Request.Path);
        };
    });

    app.UseExceptionHandler();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
        .AllowAnonymous();

    app.MapAuthEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
