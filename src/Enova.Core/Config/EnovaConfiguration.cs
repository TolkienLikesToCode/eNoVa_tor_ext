namespace Lragrz.Enova.Core.Config;

/// <summary>
/// Strongly-typed configuration loaded from config.properties / environment.
/// Network-related values are not hard-coded; operators set them per deployment.
/// </summary>
public sealed class EnovaConfiguration
{
    /// <summary>Crypto provider: MOCK_FALLBACK | VDA_PRODUCTION</summary>
    public string CryptoProvider { get; set; } = "MOCK_FALLBACK";

    /// <summary>
    /// When false, inbound signature verification and outbound QES are skipped.
    /// Controlled by server (enova.crypto.signing.enabled) and pushed to clients via /api/config.
    /// Client may also set the same key locally; server value wins after successful contact.
    /// </summary>
    public bool SigningEnabled { get; set; } = false;

    public string VdaRestEndpoint { get; set; } = "https://d-trust.de";
    public string AuthorityIdentifier { get; set; } = "DE-LRAGRZ-01";

    /// <summary>URL clients use to reach the API (scheme://host:port).</summary>
    public string ServerBaseUrl { get; set; } = "http://localhost:5101";

    /// <summary>
    /// Explicit Kestrel listen URL(s), e.g. http://0.0.0.0:5101 or http://127.0.0.1:5101.
    /// Empty = derived from NetworkMode / Bind / Port.
    /// </summary>
    public string? ListenUrl { get; set; }

    /// <summary>local | lan | public | custom — selects default bind behaviour.</summary>
    public string NetworkMode { get; set; } = "local";

    /// <summary>Interface/host to bind (e.g. 127.0.0.1, 0.0.0.0, ::, specific IP).</summary>
    public string NetworkBind { get; set; } = "127.0.0.1";

    /// <summary>TCP port for the HTTP API.</summary>
    public int NetworkPort { get; set; } = 5101;

    /// <summary>ASP.NET AllowedHosts pattern (* or semicolon-separated hosts).</summary>
    public string AllowedHosts { get; set; } = "*";

    /// <summary>When true, honour X-Forwarded-* from a reverse proxy.</summary>
    public bool ForwardedHeaders { get; set; } = true;

    /// <summary>Optional path base when hosted under a sub-path (e.g. /enova).</summary>
    public string? PathBase { get; set; }

    public bool UploadEnabled { get; set; } = false;
    public string UploadApiEndpoint { get; set; } = "http://localhost:5101/api/documents";
    public string SenderName { get; set; } = "LRA-GRZ";

    public string LocalOutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "eNoVa_tor");

    public string OutputAntworten { get; set; } = "antworten";
    public string OutputAblage { get; set; } = "ablage";
    public bool SeedAll { get; set; } = true;
    public string? ConfigFilePath { get; set; }

    /// <summary>
    /// Resolve the effective Kestrel listen URL from env, explicit ListenUrl, or mode/bind/port.
    /// Priority: ENOVA_URLS / ASPNETCORE_URLS → ListenUrl → NetworkMode/Bind/Port.
    /// </summary>
    public string ResolveListenUrls()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ENOVA_URLS")
                      ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        if (!string.IsNullOrWhiteSpace(ListenUrl))
            return ListenUrl.Trim();

        var bind = Environment.GetEnvironmentVariable("ENOVA_BIND") ?? NetworkBind;
        var portStr = Environment.GetEnvironmentVariable("ENOVA_PORT");
        var port = NetworkPort;
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var p) && p > 0)
            port = p;

        var mode = (Environment.GetEnvironmentVariable("ENOVA_NETWORK_MODE") ?? NetworkMode)
            .Trim().ToLowerInvariant();

        // Mode supplies defaults only when bind was left at the property default sense
        switch (mode)
        {
            case "local":
                bind = string.IsNullOrWhiteSpace(bind) ? "127.0.0.1" : bind;
                if (bind is "0.0.0.0" or "*") bind = "127.0.0.1";
                break;
            case "lan":
            case "public":
                if (string.IsNullOrWhiteSpace(bind) || bind is "127.0.0.1" or "localhost")
                    bind = "0.0.0.0";
                break;
            case "custom":
            default:
                if (string.IsNullOrWhiteSpace(bind))
                    bind = "127.0.0.1";
                break;
        }

        return $"http://{bind}:{port}";
    }
}
