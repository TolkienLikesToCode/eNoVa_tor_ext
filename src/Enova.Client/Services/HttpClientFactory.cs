using System;
using System.Net;
using System.Net.Http;

namespace Lragrz.Enova.Client.Services;

internal static class EnovaHttp
{
    private static readonly object Gate = new();
    private static HttpClient? _shared;

    /// <summary>Shared pooled HttpClient – never dispose.</summary>
    public static HttpClient Shared
    {
        get
        {
            if (_shared is not null) return _shared;
            lock (Gate)
            {
                return _shared ??= Build(TimeSpan.FromSeconds(15));
            }
        }
    }

    public static HttpClient Create(TimeSpan? timeout = null) => Build(timeout ?? TimeSpan.FromSeconds(15));

    private static HttpClient Build(TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 32,
            ConnectTimeout = TimeSpan.FromSeconds(3),
            EnableMultipleHttp2Connections = true
        };

        return new HttpClient(handler)
        {
            Timeout = timeout,
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
    }

    /// <summary>Rewrite localhost → 127.0.0.1 to avoid IPv6/DNS stalls on Windows.</summary>
    public static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return baseUrl;
        return baseUrl
            .Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
    }
}
