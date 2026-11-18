# Network & deployment configuration (server)

All bind addresses, ports, and public URLs are **configuration or environment variables**.
The binary does not hard-code network endpoints.

## Priority (highest first)

1. Environment variables (`ENOVA_URLS`, `ENOVA_BIND`, `ENOVA_PORT`, `ENOVA_BASEURL`, …)
2. `config/eNoVa_tor.server.properties` (or next to the server executable)
3. Built-in defaults (`local` → `http://127.0.0.1:5101`)

## Server properties

| Key | Meaning |
|-----|---------|
| `enova.network.mode` | `local` \| `lan` \| `public` \| `custom` |
| `enova.network.bind` | Interface: `127.0.0.1`, `0.0.0.0`, `::`, or a specific IP |
| `enova.network.port` | TCP port (default `5101`) |
| `enova.server.listenurl` | Optional full URL(s); if set, overrides mode/bind/port |
| `enova.server.baseurl` | URL **clients** use (may be HTTPS hostname behind a proxy) |
| `enova.network.allowedhosts` | Host-header allow list (`*` or list) |
| `enova.network.forwardedheaders` | Honour `X-Forwarded-*` (`true`/`false`) |
| `enova.network.pathbase` | Optional sub-path (e.g. `/enova`) |
| `enova.crypto.signing.enabled` | `false` = no signature verify/sign (default) |

### Mode behaviour

| Mode | Typical bind |
|------|----------------|
| `local` | `127.0.0.1` only (same machine / reverse-proxy on localhost) |
| `lan` | `0.0.0.0` — reachable on the internal network |
| `public` | `0.0.0.0` — internet-facing **only** with TLS proxy + firewall |
| `custom` | Exactly `enova.network.bind` + `enova.network.port` |

### Environment overrides

```text
ENOVA_URLS=http://0.0.0.0:5101
ENOVA_BIND=0.0.0.0
ENOVA_PORT=5101
ENOVA_BASEURL=https://enova.example.local
ENOVA_NETWORK_MODE=lan
ENOVA_SIGNING_ENABLED=false
ENOVA_ALLOWED_HOSTS=*
ENOVA_DB=Host=...;Port=...;Database=enova;Username=...;Password=...
```

## Client

The client **never discovers** the server. Set before first run:

```properties
# eNoVa_tor.client.properties (next to client EXE or in config/)
enova.server.baseurl=https://enova.example.local
enova.crypto.signing.enabled=false
```

After login, `/api/config` may update `signingEnabled` and related settings from the server.

## TLS

Prefer binding Kestrel to `127.0.0.1` and terminating TLS on a reverse proxy (Caddy, nginx, IIS, Traefik).
Set `enova.server.baseurl` to the public `https://…` URL and keep `enova.network.forwardedheaders=true`.

## Signing off (default)

```properties
enova.crypto.signing.enabled=false
```

- Client opens XML **without** requiring a notary signature.
- Answer export is **unsigned** pure XML + PDF.
- Set `true` on the **server** and restart; clients pick it up on next login/config refresh.
