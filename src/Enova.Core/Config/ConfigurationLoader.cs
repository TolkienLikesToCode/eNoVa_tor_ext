using System.Text;

namespace Lragrz.Enova.Core.Config;

/// <summary>
/// Loads key=value style properties (Java-compatible) plus optional overrides.
/// </summary>
public static class ConfigurationLoader
{
    public static EnovaConfiguration Load(string? explicitPath = null, string fileName = "eNoVa_tor.client.properties")
    {
        var config = new EnovaConfiguration();

        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(explicitPath))
            candidates.Add(explicitPath);

        var assemblyDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(assemblyDir, fileName));
        candidates.Add(Path.Combine(assemblyDir, "config", fileName));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), fileName));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "config", fileName));

        string? found = candidates.FirstOrDefault(File.Exists);

        if (found is null)
        {
            var defaultPath = Path.Combine(assemblyDir, fileName);
            try
            {
                var content = fileName.Contains("server", StringComparison.OrdinalIgnoreCase)
                    ? DefaultServerConfig
                    : DefaultClientConfig;
                File.WriteAllText(defaultPath, content, Encoding.UTF8);
                found = defaultPath;
            }
            catch
            {
                return config;
            }
        }

        config.ConfigFilePath = found;
        foreach (var raw in File.ReadAllLines(found, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var idx = line.IndexOf('=');
            if (idx <= 0)
                continue;

            Apply(config, line[..idx].Trim(), line[(idx + 1)..].Trim());
        }

        // Environment overrides (deployment without editing files)
        ApplyEnvOverrides(config);
        return config;
    }

    private static void ApplyEnvOverrides(EnovaConfiguration config)
    {
        var baseUrl = Environment.GetEnvironmentVariable("ENOVA_BASEURL")
                      ?? Environment.GetEnvironmentVariable("ENOVA_SERVER_BASEURL");
        if (!string.IsNullOrWhiteSpace(baseUrl))
            config.ServerBaseUrl = baseUrl.Trim();

        var mode = Environment.GetEnvironmentVariable("ENOVA_NETWORK_MODE");
        if (!string.IsNullOrWhiteSpace(mode))
            config.NetworkMode = mode.Trim();

        var bind = Environment.GetEnvironmentVariable("ENOVA_BIND");
        if (!string.IsNullOrWhiteSpace(bind))
            config.NetworkBind = bind.Trim();

        var port = Environment.GetEnvironmentVariable("ENOVA_PORT");
        if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var p) && p > 0)
            config.NetworkPort = p;

        var listen = Environment.GetEnvironmentVariable("ENOVA_URLS")
                     ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(listen))
            config.ListenUrl = listen.Trim();

        var signing = Environment.GetEnvironmentVariable("ENOVA_SIGNING_ENABLED");
        if (!string.IsNullOrWhiteSpace(signing))
            config.SigningEnabled = signing.Equals("true", StringComparison.OrdinalIgnoreCase) || signing == "1";

        var hosts = Environment.GetEnvironmentVariable("ENOVA_ALLOWED_HOSTS");
        if (!string.IsNullOrWhiteSpace(hosts))
            config.AllowedHosts = hosts.Trim();
    }

    private static void Apply(EnovaConfiguration config, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "enova.crypto.provider":
                config.CryptoProvider = value;
                break;
            case "enova.crypto.signing.enabled":
                config.SigningEnabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                break;
            case "enova.vda.rest.endpoint":
                config.VdaRestEndpoint = value;
                break;
            case "enova.authority.identifier":
                config.AuthorityIdentifier = value;
                break;
            case "enova.sender.name":
                config.SenderName = value;
                break;
            case "enova.server.baseurl":
                config.ServerBaseUrl = value;
                break;
            case "enova.server.listenurl":
                config.ListenUrl = value;
                break;
            case "enova.network.mode":
                config.NetworkMode = value;
                break;
            case "enova.network.bind":
                config.NetworkBind = value;
                break;
            case "enova.network.port":
                if (int.TryParse(value, out var port) && port > 0)
                    config.NetworkPort = port;
                break;
            case "enova.network.allowedhosts":
                config.AllowedHosts = value;
                break;
            case "enova.network.forwardedheaders":
                config.ForwardedHeaders = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                break;
            case "enova.network.pathbase":
                config.PathBase = string.IsNullOrWhiteSpace(value) ? null : value;
                break;
            case "enova.seed.all":
                config.SeedAll = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
                break;
            case "enova.upload.enabled":
                config.UploadEnabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                break;
            case "enova.upload.api.endpoint":
                config.UploadApiEndpoint = value;
                break;
            case "enova.local.output.directory":
                var expanded = Environment.GetEnvironmentVariables().Count >= 0
                    ? Environment.ExpandEnvironmentVariables(value)
                    : value;
                try { config.LocalOutputDirectory = Path.GetFullPath(expanded); }
                catch { config.LocalOutputDirectory = expanded; }
                break;
            case "enova.output.antworten":
                config.OutputAntworten = value;
                break;
            case "enova.output.ablage":
                config.OutputAblage = value;
                break;
        }
    }

    private const string DefaultClientConfig = """
        # eNoVa_tor – Client (auto-generated)
        # Only required setting: where the API lives
        enova.server.baseurl=http://localhost:5101
        # Optional local override (server value wins after login)
        enova.crypto.signing.enabled=false
        enova.local.output.directory=%USERPROFILE%\\Documents\\eNoVa_tor
        enova.output.antworten=antworten
        enova.output.ablage=ablage
        """;

    private const string DefaultServerConfig = """
        # eNoVa_tor – Server (auto-generated)
        enova.crypto.provider=MOCK_FALLBACK
        enova.crypto.signing.enabled=false
        enova.vda.rest.endpoint=https://d-trust.de
        enova.authority.identifier=DE-LRAGRZ-01
        enova.sender.name=LRA-GRZ
        enova.server.baseurl=http://localhost:5101
        enova.network.mode=local
        enova.network.bind=127.0.0.1
        enova.network.port=5101
        enova.network.allowedhosts=*
        enova.network.forwardedheaders=true
        enova.upload.enabled=false
        enova.upload.api.endpoint=http://localhost:5101/api/documents
        enova.seed.all=true
        """;
}
