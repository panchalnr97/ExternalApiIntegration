using System.Net;
using System.Text.Json;
using ExternalApiIntegration.Configuration;
using ExternalApiIntegration.Interfaces;
using ExternalApiIntegration.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExternalApiIntegration.Services
{
    public class ExternalUserService : IExternalUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ReqresApiOptions _options;
        private readonly IMemoryCache _cache;

        public ExternalUserService(HttpClient httpClient, IOptions<ReqresApiOptions> options, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _cache = memoryCache;

            if (!string.IsNullOrWhiteSpace(_options.ApiKey) &&
                !_httpClient.DefaultRequestHeaders.Contains("x-api-key"))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            }
        }
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            const string allKey = "all_users";
            if (_cache.TryGetValue(allKey, out IEnumerable<User> cached))
                return cached;

            var result = new List<User>();
            int page = 1, totalPages;

            try
            {
                do
                {
                    var resp = await _httpClient.GetAsync($"{_options.BaseUrl}/users?page={page}");
                    resp.EnsureSuccessStatusCode();

                    var json = await resp.Content.ReadAsStringAsync();
                    var ulr = JsonSerializer.Deserialize<UserListResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (ulr?.Data == null)
                        throw new JsonException($"Missing user Data on page {page}.");

                    result.AddRange(ulr.Data);
                    totalPages = ulr.Total_Pages;
                    page++;

                } while (page <= totalPages);

                _cache.Set(allKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error occurred while fetching all users.", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to deserialize users list.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred in GetAllUsersAsync.", ex);
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            string cacheKey = $"user_{userId}";
            if (_cache.TryGetValue(cacheKey, out User cached))
                return cached;

            try
            {
                var resp = await _httpClient.GetAsync($"{_options.BaseUrl}/users/{userId}");
                if (resp.StatusCode == HttpStatusCode.NotFound)
                    throw new KeyNotFoundException($"User {userId} not found.");

                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("Data", out var dataElement))
                    throw new JsonException("Missing 'Data' property in response.");

                var user = JsonSerializer.Deserialize<User>(dataElement.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
                return user!;
            }
            catch (KeyNotFoundException) { throw; }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error occurred while fetching user Data.", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse user Data from API response.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred in GetUserByIdAsync.", ex);
            }
        }

    }
}
