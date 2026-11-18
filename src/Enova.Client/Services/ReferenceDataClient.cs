using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Lragrz.Enova.Core.Config;

namespace Lragrz.Enova.Client.Services;

public sealed class ReferenceDataClient
{
    private readonly HttpClient _http;
    private readonly EnovaConfiguration _config;

    private static readonly ConcurrentDictionary<string, (DateTime At, object Data)> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public ReferenceDataClient(EnovaConfiguration config, HttpClient? http = null)
    {
        _config = config;
        _http = http ?? EnovaHttp.Shared;
    }

    public void ClearCache() => Cache.Clear();

    private string Base => EnovaHttp.NormalizeBaseUrl(_config.ServerBaseUrl);

    public async Task<List<CaseworkerDto>> GetCaseworkersAsync(string token, CancellationToken ct = default)
    {
        const string key = "caseworkers";
        if (TryGet<List<CaseworkerDto>>(key, out var hit)) return hit!;

        var list = await GetJsonAsync<List<CaseworkerDto>>($"{Base}/api/caseworkers", token, ct).ConfigureAwait(false)
                   ?? new List<CaseworkerDto>();
        Cache[key] = (DateTime.UtcNow, list);
        return list;
    }

    public async Task<Dictionary<string, List<string>>> GetDecisionMappingsAsync(string token, CancellationToken ct = default)
    {
        const string key = "decision-mappings";
        if (TryGet<Dictionary<string, List<string>>>(key, out var hit)) return hit!;

        var map = await GetJsonAsync<Dictionary<string, List<string>>>($"{Base}/api/decision-mappings", token, ct).ConfigureAwait(false)
                  ?? new Dictionary<string, List<string>>();
        Cache[key] = (DateTime.UtcNow, map);
        return map;
    }

    public async Task<List<CodeListEntryDto>> GetCodeListAsync(string listId, string token, CancellationToken ct = default)
    {
        var key = "codelist:" + listId;
        if (TryGet<List<CodeListEntryDto>>(key, out var hit)) return hit!;

        var list = await GetJsonAsync<List<CodeListEntryDto>>($"{Base}/api/codelists/{listId}", token, ct).ConfigureAwait(false)
                   ?? new List<CodeListEntryDto>();
        Cache[key] = (DateTime.UtcNow, list);
        return list;
    }

    private static bool TryGet<T>(string key, out T? value) where T : class
    {
        value = null;
        if (!Cache.TryGetValue(key, out var entry)) return false;
        if (DateTime.UtcNow - entry.At > CacheTtl)
        {
            Cache.TryRemove(key, out _);
            return false;
        }
        value = entry.Data as T;
        return value is not null;
    }

    private async Task<T?> GetJsonAsync<T>(string url, string token, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct)
            .ConfigureAwait(false);
    }
}

public sealed record CaseworkerDto(string Key, string DisplayName, string Vorname, string Nachname);
public sealed record CodeListEntryDto(string Code, string Label);
