using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Services
{
    // All MVC-to-API communication passes through this typed client.
    // The API's session cookie is stored in the current MVC session, not shared
    // in a global CookieContainer, so one visitor cannot reuse another visitor's API login.
    // Reference: https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient
    public sealed class RaceDayApiClient
    {
        private const string ApiCookieSessionKey = "RaceDayApiSessionCookie";
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public RaceDayApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Put, path)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public async Task PostNoContentAsync(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path);
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, path);
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<T> UploadAsync<T>(string path, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new RaceDayApiException((int)HttpStatusCode.BadRequest, "Choose an image file to upload.");
            }

            using MultipartFormDataContent multipart = new();
            await using Stream fileStream = file.OpenReadStream();
            using StreamContent streamContent = new(fileStream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            multipart.Add(streamContent, "file", Path.GetFileName(file.FileName));

            using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = multipart };
            using HttpResponseMessage response = await SendAsync(request, cancellationToken);
            return await ReadAsync<T>(response, cancellationToken);
        }

        public void ClearApiSessionCookie()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(ApiCookieSessionKey);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpContext context = _httpContextAccessor.HttpContext;
            string apiCookie = context?.Session.GetString(ApiCookieSessionKey);
            if (!string.IsNullOrWhiteSpace(apiCookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new RaceDayApiException((int)HttpStatusCode.ServiceUnavailable,
                    "The RaceDay API is unavailable. Start the API service and try again.");
            }

            // Capture only the cookie pair, not path/security attributes, then send it back on the next API call.
            if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookieHeaders))
            {
                string cookiePair = cookieHeaders
                    .Select(value => value.Split(new[] { ';' }, 2)[0].Trim())
                    .FirstOrDefault(value => value.StartsWith("RaceDay.Session=", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(cookiePair))
                {
                    context?.Session.SetString(ApiCookieSessionKey, cookiePair);
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                context?.Session.Remove(ApiCookieSessionKey);
            }

            return response;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await EnsureSuccessAsync(response, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string message = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"The RaceDay API returned HTTP {(int)response.StatusCode}.";
            }
            else
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(message);
                    JsonElement root = document.RootElement;
                    if (root.ValueKind == JsonValueKind.String)
                    {
                        message = root.GetString() ?? message;
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        bool hasValidationErrors = false;
                        if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            List<string> validationMessages = errors.EnumerateObject()
                                .SelectMany(item => item.Value.ValueKind == JsonValueKind.Array
                                    ? item.Value.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString() ?? string.Empty)
                                    : Enumerable.Empty<string>())
                                .Where(value => !string.IsNullOrWhiteSpace(value))
                                .ToList();

                            if (validationMessages.Count > 0)
                            {
                                message = string.Join(" ", validationMessages);
                                hasValidationErrors = true;
                            }
                        }

                        if (!hasValidationErrors && root.TryGetProperty("detail", out JsonElement detail) && detail.ValueKind == JsonValueKind.String)
                        {
                            message = detail.GetString() ?? message;
                        }
                        else if (!hasValidationErrors && root.TryGetProperty("title", out JsonElement title) && title.ValueKind == JsonValueKind.String)
                        {
                            message = title.GetString() ?? message;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Keep a plain-text API response unchanged.
                }
            }

            throw new RaceDayApiException((int)response.StatusCode, message);
        }
    }
}
