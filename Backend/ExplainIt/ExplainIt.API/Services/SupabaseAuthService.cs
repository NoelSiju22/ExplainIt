using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExplainIt.API.Services
{
    public class SupabaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SupabaseAuthService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }


        public async Task<JsonElement?> RegisterAsync(string name, string email, string password)
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var publishableKey = _configuration["Supabase:PublishableKey"];
            if (string.IsNullOrWhiteSpace(supabaseUrl))
                throw new InvalidOperationException("Supabase URL is not configured.");
            if (string.IsNullOrWhiteSpace(publishableKey))
                throw new InvalidOperationException("Supabase publishable key is not configured.");

            var url = $"{supabaseUrl.TrimEnd('/')}/auth/v1/signup";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", publishableKey);
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                email, password, data = new { full_name = name }
            }), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                string message = "Unable to register this account.";
                try { using var errorDoc = JsonDocument.Parse(responseBody); if (errorDoc.RootElement.TryGetProperty("msg", out var msg)) message = msg.GetString() ?? message; else if (errorDoc.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? message; else if (errorDoc.RootElement.TryGetProperty("error_description", out var d)) message = d.GetString() ?? message; } catch { }
                throw new InvalidOperationException(message);
            }
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.Clone();
        }

        public async Task<JsonElement?> LoginAsync(
            string email,
            string password)
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var publishableKey = _configuration["Supabase:PublishableKey"];

            if (string.IsNullOrWhiteSpace(supabaseUrl))
                throw new InvalidOperationException(
                    "Supabase URL is not configured.");

            if (string.IsNullOrWhiteSpace(publishableKey))
                throw new InvalidOperationException(
                    "Supabase publishable key is not configured.");

            var url =
                $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password";

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            request.Headers.Add("apikey", publishableKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    email,
                    password
                }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document =
                JsonDocument.Parse(responseBody);

            return document.RootElement.Clone();
        }
    }
}