# eNoVa_tor – Änderungs- und Betriebspaket

Dieses Verzeichnis enthält die geänderten Quellen, plattformspezifische Skripte und die Betriebsdokumentation.

## Plattformen

| | Windows 11 | Linux | macOS |
|--|:----------:|:-----:|:-----:|
| **Server** + PostgreSQL (Docker) | ja | ja | ja |
| **Client** (WPF) | ja | – | – |

## Inhalt

| Pfad | Beschreibung |
|------|----------------|
| `config/` | Server- und Client-Properties (Signing, Listen-URL) |
| `src/` | Geänderte C#-Quellen |
| `tests/` | Unittests |
| `docs/ARBEITSHILFE.md` | Install, Netzwerk, TLS, **Windows / Linux / macOS** |
| `docs/Caddyfile.example` | TLS mit Caddy |
| `docs/nginx-enova.conf.example` | TLS mit nginx |
| `docs/open-firewall-port.ps1` | Windows-Firewall |
| `scripts/windows/` | `setup-db.ps1`, `publish-all.ps1`, `run-server.ps1` |
| `scripts/linux/` | `install-deps.sh`, `setup-db.sh`, `publish-server.sh`, `run-server.sh` |
| `scripts/macos/` | `install-deps.sh`, `setup-db.sh`, `publish-server.sh`, `run-server.sh` |

## Externer Zugriff

```properties
enova.server.listenurl=http://127.0.0.1:5101
# oder: http://0.0.0.0:5101
```

```bash
export ENOVA_URLS=http://0.0.0.0:5101
```

Details: **docs/ARBEITSHILFE.md** (Abschnitte 3–5 und 12).
