using System.Security.Claims;
using NajaEcho.Api.Features.Characters.Contracts;
using NajaEcho.Application.Features.Characters.GetCharacters;
using NajaEcho.Application.Features.Characters.GetRegistration;
using NajaEcho.Application.Features.Characters.StartRegistration;
using NajaEcho.Application.Features.Characters.VerifyCharacter;
using Serilog;

namespace NajaEcho.Api.Features.Characters;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters").RequireAuthorization();

        group.MapGet("/", GetCharacters);
        group.MapGet("/registration", GetRegistration);
        group.MapPost("/registration", StartRegistration);
        group.MapPost("/verify", VerifyCharacter);

        return app;
    }

    // ── GET /api/characters ───────────────────────────────────────────────

    private static async Task<IResult> GetCharacters(
        ClaimsPrincipal user,
        GetCharactersHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        Log.Information("GetCharacters {CallerId}", callerId);

        try
        {
            var characters = await handler.HandleAsync(new GetCharactersQuery(callerId), ct);
            var response = new CharacterListResponse(
                characters.Select(c => new CharacterResponse(c.Id, c.Name, c.Handle, c.CreatedAt)).ToList());
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetCharacters {CallerId} failed", callerId);
            return Results.Problem("An unexpected error occurred.", statusCode: 500, title: "Internal server error");
        }
    }

    // ── GET /api/characters/registration ─────────────────────────────────

    private static async Task<IResult> GetRegistration(
        ClaimsPrincipal user,
        GetRegistrationHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        Log.Information("GetRegistration {CallerId}", callerId);

        try
        {
            var dto = await handler.HandleAsync(new GetRegistrationQuery(callerId), ct);
            if (dto is null) return Results.Ok((PendingRegistrationResponse?)null);
            return Results.Ok(new PendingRegistrationResponse(dto.Token, dto.ExpiresAt));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetRegistration {CallerId} failed", callerId);
            return Results.Problem("An unexpected error occurred.", statusCode: 500, title: "Internal server error");
        }
    }

    // ── POST /api/characters/registration ────────────────────────────────

    private static async Task<IResult> StartRegistration(
        ClaimsPrincipal user,
        StartRegistrationHandler handler,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        Log.Information("StartRegistration {CallerId}", callerId);

        try
        {
            var dto = await handler.HandleAsync(new StartRegistrationCommand(callerId), ct);
            var response = new StartRegistrationResponse(dto.Token, dto.ExpiresAt);
            Log.Information("StartRegistration {CallerId} token issued (expires {ExpiresAt})", callerId, dto.ExpiresAt);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "StartRegistration {CallerId} failed", callerId);
            return Results.Problem("An unexpected error occurred.", statusCode: 500, title: "Internal server error");
        }
    }

    // ── POST /api/characters/verify ───────────────────────────────────────

    private static async Task<IResult> VerifyCharacter(
        ClaimsPrincipal user,
        VerifyCharacterHandler handler,
        VerifyCharacterRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(user, out var callerId)) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Handle))
            return Results.Problem("Handle must not be empty.", statusCode: 400, title: "Validation error");

        Log.Information("VerifyCharacter {CallerId} handle={Handle}", callerId, request.Handle);

        try
        {
            var dto = await handler.HandleAsync(new VerifyCharacterCommand(callerId, request.Handle), ct);
            var response = new CharacterResponse(dto.Id, dto.Name, dto.Handle, dto.CreatedAt);
            Log.Information("VerifyCharacter {CallerId} character created id={CharacterId}", callerId, dto.Id);
            return Results.Created($"/api/characters/{dto.Id}", response);
        }
        catch (TokenExpiredException ex)
        {
            return Results.Problem(ex.Message, statusCode: 409, title: "Token expired — please start a new registration");
        }
        catch (TokenNotFoundException ex)
        {
            return Results.Problem(ex.Message, statusCode: 422, title: "Token not found on your RSI profile");
        }
        catch (HandleAlreadyClaimedException ex)
        {
            return Results.Problem(ex.Message, statusCode: 409, title: "This handle is already claimed");
        }
        catch (RsiProfileNotFoundException ex)
        {
            return Results.Problem(ex.Message, statusCode: 404, title: "RSI citizen profile not found for that handle");
        }
        catch (RsiUnreachableException ex)
        {
            return Results.Problem(ex.Message, statusCode: 502, title: "Could not reach RSI — please try again shortly");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "VerifyCharacter {CallerId} unexpected error", callerId);
            return Results.Problem("An unexpected error occurred.", statusCode: 500, title: "Internal server error");
        }
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
