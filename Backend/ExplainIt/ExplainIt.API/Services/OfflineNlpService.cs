using System.Net.Http.Json;
using System.Text.Json;

namespace ExplainIt.API.Services;

/// <summary>Calls ExplainIt's local Python NLP service. No cloud AI provider is used.</summary>
public sealed class OfflineNlpService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<JsonDocument> AnalyzeAsync(string text, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "analyze",
            new AnalyzeTextRequest(text),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = body.Length > 1200 ? body[..1200] : body;
            throw new HttpRequestException(
                $"Local NLP returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
        }

        return JsonDocument.Parse(body);
    }

    private sealed record AnalyzeTextRequest(string Text);
}
