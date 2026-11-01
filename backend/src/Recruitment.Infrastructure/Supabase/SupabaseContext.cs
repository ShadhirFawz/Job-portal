using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Recruitment.Infrastructure.Supabase;

public class SupabaseContext : ISupabaseContext
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SupabaseContext> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SupabaseContext(
        HttpClient httpClient,
        IOptions<SupabaseSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SupabaseContext> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.AnonKey))
        {
            throw new InvalidOperationException(
                "Supabase configuration is missing. Please set Supabase:Url and Supabase:AnonKey in appsettings.Local.json, user-secrets, or environment variables.");
        }

        _httpClient.BaseAddress = new Uri(_settings.Url.TrimEnd('/') + "/");
    }

    private void ApplyHeaders(HttpRequestMessage request, bool requireRepresentation = false)
    {
        request.Headers.Clear();
        request.Headers.Add("apikey", _settings.AnonKey);

        // Forward incoming user Bearer token to Supabase for RLS auth.uid() execution
        var userToken = GetBearerToken();
        var authToken = !string.IsNullOrWhiteSpace(userToken) ? userToken : _settings.AnonKey;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        if (requireRepresentation)
        {
            request.Headers.Add("Prefer", "return=representation");
        }
    }

    private string? GetBearerToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            !string.IsNullOrWhiteSpace(authHeader.ToString()))
        {
            var headerStr = authHeader.ToString().Trim();
            if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return headerStr["Bearer ".Length..].Trim();
            }
            return headerStr;
        }

        if (httpContext.Request.Headers.TryGetValue("x-supabase-token", out var sbHeader) &&
            !string.IsNullOrWhiteSpace(sbHeader.ToString()))
        {
            return sbHeader.ToString().Trim();
        }

        return null;
    }

    public async Task<T?> GetSingleAsync<T>(string tableQuery)
    {
        var url = $"rest/v1/{tableQuery}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return default;

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Supabase GET error: {Status} - {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Supabase query error: {content}", null, response.StatusCode);
        }

        // If returned as an array, return the first item or deserialize directly
        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            if (doc.RootElement.GetArrayLength() == 0) return default;
            return JsonSerializer.Deserialize<T>(doc.RootElement[0].GetRawText(), JsonOptions);
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public async Task<List<T>> GetListAsync<T>(string tableQuery)
    {
        var url = $"rest/v1/{tableQuery}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Supabase GET List error: {Status} - {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Supabase query error: {content}", null, response.StatusCode);
        }

        return JsonSerializer.Deserialize<List<T>>(content, JsonOptions) ?? [];
    }

    public async Task<T> InsertAsync<T>(string table, object payload)
    {
        var url = $"rest/v1/{table}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyHeaders(request, requireRepresentation: true);

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Supabase INSERT error on {Table}: {Status} - {Content}", table, response.StatusCode, content);
            throw new HttpRequestException($"Supabase insert error: {content}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            if (doc.RootElement.GetArrayLength() == 0)
                throw new InvalidOperationException("Insert succeeded but no record returned.");
            return JsonSerializer.Deserialize<T>(doc.RootElement[0].GetRawText(), JsonOptions)!;
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }

    public async Task<T?> UpdateAsync<T>(string tableQuery, object payload)
    {
        var url = $"rest/v1/{tableQuery}";
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        ApplyHeaders(request, requireRepresentation: true);

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Supabase UPDATE error on {Query}: {Status} - {Content}", tableQuery, response.StatusCode, content);
            throw new HttpRequestException($"Supabase update error: {content}", null, response.StatusCode);
        }

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            if (doc.RootElement.GetArrayLength() == 0) return default;
            return JsonSerializer.Deserialize<T>(doc.RootElement[0].GetRawText(), JsonOptions);
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public async Task DeleteAsync(string tableQuery)
    {
        var url = $"rest/v1/{tableQuery}";
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        ApplyHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError("Supabase DELETE error on {Query}: {Status} - {Content}", tableQuery, response.StatusCode, content);
            throw new HttpRequestException($"Supabase delete error: {content}", null, response.StatusCode);
        }
    }

    public async Task<T?> RpcAsync<T>(string functionName, object? payload = null)
    {
        var url = $"rest/v1/rpc/{functionName}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyHeaders(request);

        var json = payload != null ? JsonSerializer.Serialize(payload, JsonOptions) : "{}";
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase RPC {Func} returned: {Status} - {Content}", functionName, response.StatusCode, content);
            throw new HttpRequestException($"Supabase RPC error: {content}", null, response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content)) return default;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public async Task RpcVoidAsync(string functionName, object? payload = null)
    {
        var url = $"rest/v1/rpc/{functionName}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyHeaders(request);

        var json = payload != null ? JsonSerializer.Serialize(payload, JsonOptions) : "{}";
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Supabase RPC {Func} returned: {Status} - {Content}", functionName, response.StatusCode, content);
            throw new HttpRequestException($"Supabase RPC error: {content}", null, response.StatusCode);
        }
    }

    public async Task<string?> CreateSignedUrlAsync(string bucket, string path, int expiresIn = 3600)
    {
        var url = $"storage/v1/object/sign/{bucket}/{path.TrimStart('/')}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyHeaders(request);

        var body = JsonSerializer.Serialize(new { expiresIn });
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError("Supabase Storage signed URL error: {Status} - {Content}", response.StatusCode, content);
            return null;
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resultJson);
        if (doc.RootElement.TryGetProperty("signedURL", out var signedUrlElement))
        {
            var signedUrl = signedUrlElement.GetString();
            if (string.IsNullOrEmpty(signedUrl)) return null;

            if (signedUrl.StartsWith("http://") || signedUrl.StartsWith("https://"))
                return signedUrl;

            return $"{_settings.Url.TrimEnd('/')}/storage/v1{signedUrl}";
        }

        return null;
    }

    public async Task DeleteStorageObjectAsync(string bucket, string path)
    {
        var url = $"storage/v1/object/{bucket}";
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        ApplyHeaders(request);

        var body = JsonSerializer.Serialize(new { prefixes = new[] { path.TrimStart('/') } });
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Supabase Storage delete object error: {Status} - {Content}", response.StatusCode, content);
        }
    }
}
