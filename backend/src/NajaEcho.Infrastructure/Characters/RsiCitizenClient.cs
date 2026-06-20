using AngleSharp;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;

namespace NajaEcho.Infrastructure.Characters;

public sealed class RsiCitizenClient(HttpClient http, ILogger<RsiCitizenClient> logger) : IRsiCitizenClient
{
    public async Task<object> FetchCitizenAsync(string handle, CancellationToken ct)
    {
        logger.LogInformation("RsiCitizenClient fetching citizen handle={Handle}", handle);

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync($"en/citizens/{handle}", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogWarning("RsiCitizenClient unreachable for handle={Handle}", handle);
            return new RsiUnreachable();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("RsiCitizenClient citizen not found handle={Handle}", handle);
            return new RsiProfileNotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("RsiCitizenClient unexpected status={Status} handle={Handle}", (int)response.StatusCode, handle);
            return new RsiUnreachable();
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var displayName = ParseCommunityMoniker(content);

        logger.LogInformation("RsiCitizenClient fetched handle={Handle} displayName={DisplayName}", handle, displayName);

        return new RsiCitizenPage(content, displayName);
    }

    private static string? ParseCommunityMoniker(string html)
    {
        try
        {
            var parser = new HtmlParser();
            using var document = parser.ParseDocument(html);

            // RSI citizen page has the community moniker in div.info > p.entry with preceding div.label "Handle name" sibling
            // The primary identity heading or the community moniker block uses specific selectors.
            // Based on R1/R4: the Community Moniker is the primary display name (e.g. "G8trdone" for handle "g8r").
            // It appears in the heading or in an .entry paragraph in the identity block.
            var monikerEl = document.QuerySelector(".profile-content .info .value")
                ?? document.QuerySelector(".public-profile .info-block .value")
                ?? document.QuerySelector("[class*='moniker']")
                ?? document.QuerySelector("h1.entry");

            return monikerEl?.TextContent?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
