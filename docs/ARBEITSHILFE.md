# Arbeitshilfe – eNoVa_tor Installation & Betrieb

**Version:** 0.1 (2026)  
**Gilt für:** Server unter **Windows 11 / Linux / macOS**; Clients nur **Windows** (WPF)  
**Zielgruppe:** IT-Betrieb / Fachverfahren EGOV

Diese Arbeitshilfe beschreibt die vollständige Einrichtung von Server und Client, die Freigabe für den Netzwerkeinsatz sowie die absicherung des Datenstroms per TLS.

---

## 1. Architektur (Kurz)

| Komponente | Rolle | Port (Standard) |
|------------|--------|------------------|
| **Enova.Server** (ASP.NET Core / Kestrel) | Auth, Config, Upload-Queue, Referenzdaten | TCP **5101** |
| **PostgreSQL 16** (Docker) | Benutzer, Organisationen, Vorgänge, Queue | TCP **55432** (nur localhost) |
| **Enova.Client** (WPF) | Bearbeitung XJustiz / Antwort / PDF/A-3 | – (ruft Server auf) |
| **Reverse-Proxy (empfohlen)** | TLS-Terminierung (HTTPS) | TCP **443** |

Datenfluss Client → (HTTPS) → Proxy → (HTTP localhost) → Kestrel → PostgreSQL.

---

## 2. Voraussetzungen

### 2.1 Server-Maschine

- Windows Server 2019/2022 oder Windows 10/11 (64-bit)
- Administratorrechte für Installation
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Build) bzw. **.NET 10 Runtime / ASP.NET Core Runtime** (nur Betrieb)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) **oder** Docker Engine + Compose
- Optional: Caddy oder nginx für TLS
- Feste IP oder DNS-Name im Behördennetz (z. B. `enova.lra-intern.local`)

### 2.2 Client-Arbeitsplätze

- Windows 10/11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download) (bei self-contained Publish **nicht** nötig)
- Netzwerkzugriff auf den Server (Port 443 bzw. 5101)
- Schreibrechte unter `%USERPROFILE%\Documents\eNoVa_tor`

### 2.3 Netzwerk

- Client-Subnetz → Server: TCP 443 (HTTPS) **oder** 5101 (nur wenn ohne Proxy, nicht empfohlen im Klartext)
- PostgreSQL-Port **55432 nicht** ins Netz freigeben
- optional: Management-Zugriff SSH/RDP nur aus Admin-Netz

---

## 3. Server-Installation (Schritt für Schritt)

### 3.1 Software installieren

```powershell
# PowerShell (Admin) – Beispiel mit winget
winget install -e --id Microsoft.DotNet.SDK.10
winget install -e --id Docker.DockerDesktop
# Nach Docker-Installation: Docker Desktop starten und warten bis „Running“
```

Repository bzw. Release entpacken, z. B.:

```powershell
cd C:\Apps\eNoVa_tor
```

### 3.2 Datenbank starten

```powershell
cd C:\Apps\eNoVa_tor
.\setup.bat
# oder nur DB:
.\start-db.bat
# intern:
# docker compose -f docker\docker-compose.yml up -d
```

Prüfung:

```powershell
docker ps
# Container enova_tor sollte „healthy“ / Up sein
# Verbindung: Host=localhost Port=55432 User=enova Password=1234 DB=enova
```

**Produktion:** Passwort in Docker Compose und in der Connection-String-Umgebungsvariable ändern:

```powershell
$env:ENOVA_DB = "Host=localhost;Port=55432;Database=enova;Username=enova;Password=<StarkesPasswort>"
```

### 3.3 Server-Konfiguration

Datei: `config\eNoVa_tor.server.properties` (liegt nach Publish neben der EXE bzw. im Projekt unter `config\`).

Wichtige Schlüssel:

```properties
# Öffentliche URL, die Clients verwenden (nach TLS: https://…)
enova.server.baseurl=https://enova.lra-intern.local

# Kestrel-Bindung:
#   localhost  = nur lokal (hinter Reverse-Proxy empfohlen)
#   0.0.0.0    = alle Schnittstellen (nur wenn nötig)
enova.server.listenurl=http://127.0.0.1:5101

enova.crypto.signing.enabled=false
enova.authority.identifier=DE-LRAGRZ-01
enova.sender.name=LRA-GRZ
enova.upload.enabled=true
enova.seed.all=true
```

JWT-Schlüssel in `appsettings.json` **unbedingt** ändern:

```json
"Jwt": {
  "Key": "<mindestens-32-Zeichen-Zufall>",
  "Issuer": "eNoVa_tor",
  "Audience": "eNoVa_tor-client",
  "ExpireHours": 8
}
```

### 3.4 Server bauen und starten

```powershell
cd C:\Apps\eNoVa_tor
.\publish-server.bat
# Ausgabe: artifacts\server\Enova.Server.exe  (bzw. eNoVa_tor.Server.exe laut README)

# Alternativ Entwicklung:
dotnet run --project src\Enova.Server\Enova.Server.csproj
```

Externer Bind ohne Dateiänderung:

```powershell
$env:ENOVA_URLS = "http://0.0.0.0:5101"
dotnet run --project src\Enova.Server\Enova.Server.csproj
```

Hilfsskript (nur LAN/VPN, Klartext): `docs\run-server-external.ps1`

Standardkonten nach Seed (Passwort ändern!):

| Benutzer     | Passwort        | Rolle                  |
|--------------|-----------------|------------------------|
| admin        | ChangeMe!2026   | Organisationsverwalter |
| egov-admin   | ChangeMe!2026   | Behörden-Admin         |
| editor1      | ChangeMe!2026   | Sachbearbeiter         |

### 3.5 Firewall (Windows)

```powershell
# Admin-PowerShell – nur Behörden-Subnetz, nicht Any
cd C:\Apps\eNoVa_tor\docs
.\open-firewall-port.ps1 -Port 443 -RemoteAddress "10.0.0.0/8"
# Falls ohne Proxy und nur intern:
.\open-firewall-port.ps1 -Port 5101 -RemoteAddress "LocalSubnet"
```

Manuell:

```powershell
New-NetFirewallRule -DisplayName "eNoVa_tor HTTPS" -Direction Inbound `
  -Protocol TCP -LocalPort 443 -Action Allow -RemoteAddress LocalSubnet `
  -Profile Domain,Private
```

---

## 4. Verschlüsselter Datenstrom (TLS) – empfohlenes Setup

**Ziel:** Clients sprechen ausschließlich **HTTPS**; Kestrel bleibt auf `127.0.0.1:5101` und ist nicht direkt aus dem Netz erreichbar.

### 4.1 Variante A – Caddy (einfach, automatische Zertifikate möglich)

1. Caddy installieren: https://caddyserver.com/docs/install  
2. Beispiel: `docs\Caddyfile.example` nach `Caddyfile` kopieren und Hostnamen anpassen.  
3. Internes Zertifikat (Behörden-CA) oder ACME.

```powershell
# Beispielstart
caddy run --config C:\Apps\eNoVa_tor\docs\Caddyfile
```

Kestrel-Bindung:

```properties
enova.server.listenurl=http://127.0.0.1:5101
enova.server.baseurl=https://enova.lra-intern.local
```

### 4.2 Variante B – nginx

1. nginx für Windows oder Linux-VM vor dem API-Host.  
2. Zertifikat + Key ablegen (Behörden-PKI).  
3. Config: `docs\nginx-enova.conf.example`  
4. `nginx -t` und Reload.

### 4.3 Variante C – Nur Behörden-VPN

Wenn alle Clients über VPN (IPsec/WireGuard/Always-On) kommen:

- Kestrel kann `http://0.0.0.0:5101` binden  
- Firewall nur VPN-Pool freigeben  
- TLS trotzdem empfohlen (Defense in Depth)

### 4.4 Was TLS schützt

- Login (Benutzer/Passwort) und JWT im Transit  
- Hochgeladene PDF/A-3 und Metadaten  
- Config-API (Authority, Sender, Upload-Flags)

**Zusätzlich:**

- Starke JWT-`Key` in `appsettings.json`  
- Standardpasswörter sofort ändern  
- PostgreSQL nicht exposen  
- Optional: HSTS nur hinter echtem HTTPS-Namen

### 4.5 Kurz-Check Verschlüsselung

```powershell
# Von einem Client-PC:
curl -I https://enova.lra-intern.local/api/auth/login
# Erwartung: HTTP-Antwort der API (z. B. 405/400 o. ä.), Zertifikat vertrauenswürdig
```

---

## 5. Netzwerk-Routing / Freischaltung (Checkliste)

1. **DNS:** `enova.lra-intern.local` → Server-IP (interne Zone).  
2. **Firewall Server:** 443 (oder 5101) von Client-Subnetzen.  
3. **Firewall/Perimeter:** keine unnötige Internet-Freigabe; bei Bedarf nur VPN.  
4. **Reverse-Proxy** auf Server lauscht 443, leitet an `127.0.0.1:5101`.  
5. **Kestrel** nur localhost **oder** gezielt 0.0.0.0 hinter restriktiver Firewall.  
6. **Client-Config** `enova.server.baseurl` = dieselbe HTTPS-URL.  
7. Test: Login vom Fachverfahren-PC, nicht nur lokal auf dem Server.

Windows-Route ist i. d. R. nicht nötig, wenn Client und Server im gerouteten LAN liegen. Bei getrennten VLANs: Routing/ACL auf dem Core-Switch freigeben (Ziel-IP, TCP 443).

---

## 6. Client-Installation

### 6.1 Publish (Build-Maschine)

```powershell
cd C:\Apps\eNoVa_tor
.\publish-client.bat
# Ausgabe: artifacts\client\
```

### 6.2 Verteilung

Ordner `artifacts\client\` auf Arbeitsplätze kopieren (Softwareverteilung / manuell).  
Neben der EXE muss liegen:

- `eNoVa_tor.client.properties` bzw. `config\eNoVa_tor.client.properties`

Inhalt anpassen:

```properties
enova.server.baseurl=https://enova.lra-intern.local
enova.local.output.directory=%USERPROFILE%\Documents\eNoVa_tor
```

### 6.3 Start

```powershell
.\eNoVa_tor.Client.exe
# oder Entwicklung:
dotnet run --project src\Enova.Client\Enova.Client.csproj
```

Anmeldung mit Server-Konto. Nach Login lädt der Client `/api/config` (u. a. `signingEnabled`, Authority, Upload).

---

## 7. Signatur-Schalter (aktueller Stand)

In `eNoVa_tor.server.properties`:

```properties
enova.crypto.signing.enabled=false
```

| Wert | Wirkung |
|------|---------|
| `false` (Default) | Keine Prüfung der Notar-Signatur; Antwort-XML **ohne** QES |
| `true` | Altes Verhalten: Prüfung eingehend + Mock/VDA-Signatur ausgehend |

Änderung → Server neu starten → Clients neu anmelden (Config wird gezogen).

---

## 8. Betrieb & Wartung

```powershell
# DB-Status
docker compose -f docker\docker-compose.yml ps

# Logs (Audit)
%LocalAppData%\eNoVa_tor\logs\yyyy-MM-dd.log

# Upload-Queue (API, authentifiziert)
# GET /api/upload-queue
```

Backup: Docker-Volume `enova_pgdata` + Config-Dateien + optional Ablageordner.

---

## 9. Fehlerbilder

| Symptom | Ursache / Maßnahme |
|---------|---------------------|
| Client: „Verbindung fehlgeschlagen“ | `enova.server.baseurl` falsch; Firewall; Server nicht gestartet |
| Timeout von anderem PC, lokal OK | `listenurl` noch `localhost` → auf Proxy-Setup umstellen oder `0.0.0.0` + Firewall |
| Zertifikatsfehler | Behörden-CA auf Clients verteilen; Hostname = CN/SAN |
| 401 Login | Benutzer/Passwort; Seed; Uhrzeit (JWT) |
| 429 Login | Rate-Limit (10/Min/IP) – warten |
| DB-Fehler beim Start | Docker/Postgres, `ENOVA_DB`, Port 55432 |

---

## 10. Schnellreferenz Befehle

```powershell
# --- Server ---
cd C:\Apps\eNoVa_tor
.\setup.bat
$env:ENOVA_DB = "Host=localhost;Port=55432;Database=enova;Username=enova;Password=***"
# Hinter Proxy:
# enova.server.listenurl=http://127.0.0.1:5101
dotnet run --project src\Enova.Server\Enova.Server.csproj
# oder publish:
.\publish-server.bat

# --- Client ---
.\publish-client.bat
# properties: enova.server.baseurl=https://enova.lra-intern.local
```

```powershell
# Firewall (Admin)
.\docs\open-firewall-port.ps1 -Port 443 -RemoteAddress "10.0.0.0/8"
```

---

## 11. Lieferumfang dieser Paketdateien (`artifacts/eNoVa`)

Geänderte/ergänzte Dateien zum Übernehmen ins Projekt:

- `config/eNoVa_tor.server.properties` – inkl. `enova.server.listenurl`, Signing-Schalter  
- `config/eNoVa_tor.client.properties` – Client-URL  
- `src/Enova.Server/Program.cs` – Listen-URL aus Env/Properties  
- `src/Enova.Core/Config/*` – `SigningEnabled`, Loader  
- `src/Enova.Core/Xml/EnovaXmlProcessor.cs` – optionales Signing  
- `src/Enova.Client/MainWindow.xaml.cs`, `Services/AuthSession.cs`  
- `tests/Enova.Tests/InMemoryPipelineTests.cs`  
- `docs/ARBEITSHILFE.md` (diese Datei)  
- `docs/Caddyfile.example`, `nginx-enova.conf.example`  
- `docs/open-firewall-port.ps1`, `run-server-external.ps1`

---

*Ende der Arbeitshilfe.*


---

## 12. Plattformübergreifend: Windows 11, Linux, macOS

Der **Server** (ASP.NET Core + PostgreSQL/Docker) läuft auf allen drei Plattformen.  
Der **Client** ist **WPF** und damit **nur Windows 10/11**.

Vorgefertigte Skripte liegen unter `scripts/`:

```
scripts/
  windows/   setup-db.ps1  publish-all.ps1  run-server.ps1
  linux/     install-deps.sh  setup-db.sh  publish-server.sh  run-server.sh
  macos/     install-deps.sh  setup-db.sh  publish-server.sh  run-server.sh
```

Skripte immer aus dem **Repository-Root** aufrufen (Ordner mit `src/` und `docker/`), nachdem die Dateien aus `artifacts/eNoVa` ins Projekt übernommen wurden – oder Pfade in den Skripten anpassen.

### 12.1 Windows 11

**Voraussetzungen**

```powershell
winget install -e --id Microsoft.DotNet.SDK.10
winget install -e --id Docker.DockerDesktop
# Docker Desktop starten
```

**Datenbank**

```powershell
# Repo-Root
pwsh -File scripts\windows\setup-db.ps1
# alternativ: .\setup.bat  /  .\start-db.bat
```

**Server starten (Entwicklung)**

```powershell
pwsh -File scripts\windows\run-server.ps1
# LAN-Bind (Klartext, nur intern/VPN):
pwsh -File scripts\windows\run-server.ps1 -External
```

**Publish Server + Client**

```powershell
pwsh -File scripts\windows\publish-all.ps1
# artifacts\server\  und  artifacts\client\
```

**Firewall**

```powershell
pwsh -File docs\open-firewall-port.ps1 -Port 443 -RemoteAddress "LocalSubnet"
```

### 12.2 Linux (Debian/Ubuntu und vergleichbar)

**Voraussetzungen**

```bash
# .NET 10 SDK (offiziell):
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 10.0
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"

# Docker Engine + Compose-Plugin:
# https://docs.docker.com/engine/install/
# Benutzer in Gruppe docker:
sudo usermod -aG docker "$USER"   # danach neu anmelden

# Optional Hilfsskript:
bash scripts/linux/install-deps.sh
```

**Datenbank**

```bash
chmod +x scripts/linux/*.sh
bash scripts/linux/setup-db.sh
```

**Server starten**

```bash
bash scripts/linux/run-server.sh
# alle Interfaces:
bash scripts/linux/run-server.sh --external
# oder:
bash scripts/linux/run-server.sh --listen 'http://0.0.0.0:5101'
```

**Publish (linux-x64, self-contained)**

```bash
bash scripts/linux/publish-server.sh
# artifacts/server-linux/
./artifacts/server-linux/Enova.Server   # bzw. tatsächlicher EXE-Name
```

**Firewall (ufw-Beispiel)**

```bash
sudo ufw allow from 10.0.0.0/8 to any port 443 proto tcp
sudo ufw allow from 10.0.0.0/8 to any port 5101 proto tcp   # nur wenn ohne Proxy
sudo ufw reload
```

**systemd (optional, nach Publish)**

```ini
# /etc/systemd/system/enova-server.service
[Unit]
Description=eNoVa_tor API
After=docker.service network.target

[Service]
WorkingDirectory=/opt/enova/server-linux
Environment=ENOVA_URLS=http://127.0.0.1:5101
Environment=ENOVA_DB=Host=localhost;Port=55432;Database=enova;Username=enova;Password=***
ExecStart=/opt/enova/server-linux/Enova.Server
Restart=on-failure
User=enova

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now enova-server
```

TLS: Caddy/nginx wie in Abschnitt 4; unter Linux üblich.

### 12.3 macOS

**Voraussetzungen**

```bash
# Homebrew: https://brew.sh
bash scripts/macos/install-deps.sh
# Docker Desktop einmal manuell starten (Applications → Docker)
```

Oder manuell:

```bash
brew install dotnet          # oder: brew install --cask dotnet-sdk
brew install --cask docker
open -a Docker
```

**Datenbank**

```bash
chmod +x scripts/macos/*.sh
bash scripts/macos/setup-db.sh
```

**Server starten**

```bash
bash scripts/macos/run-server.sh
bash scripts/macos/run-server.sh --external
```

**Publish**

```bash
bash scripts/macos/publish-server.sh
# Apple Silicon → artifacts/server-osx-arm64/
# Intel         → artifacts/server-osx-x64/
```

**Firewall / Netzwerk**

- Systemeinstellungen → Netzwerk / Firewall: bei Bedarf eingehend für den Proxy (443) erlauben  
- Für LAN-Tests `--external` nutzen; produktiv Reverse-Proxy auf localhost-Kestrel

### 12.4 Was wo läuft

| Komponente | Windows 11 | Linux | macOS |
|------------|:----------:|:-----:|:-----:|
| PostgreSQL (Docker) | ja | ja | ja |
| Enova.Server | ja | ja | ja |
| Enova.Client (WPF) | **ja** | nein | nein |
| Self-contained Publish Server | win-x64 | linux-x64 | osx-arm64 / osx-x64 |
| TLS (Caddy/nginx) | ja | ja | ja |

### 12.5 Gemeinsame Konfiguration

Unabhängig vom OS:

```properties
# config/eNoVa_tor.server.properties
enova.server.listenurl=http://127.0.0.1:5101
enova.server.baseurl=https://enova.example.local
enova.crypto.signing.enabled=false
```

```bash
# Überall gültige Overrides:
export ENOVA_URLS=http://0.0.0.0:5101
export ENOVA_DB='Host=localhost;Port=55432;Database=enova;Username=enova;Password=***'
```

Client (nur Windows):

```properties
enova.server.baseurl=https://enova.example.local
```

### 12.6 Schnellmatrix „erster Start“

**Windows 11**

```powershell
winget install -e --id Microsoft.DotNet.SDK.10
winget install -e --id Docker.DockerDesktop
# Docker starten
pwsh -File scripts\windows\setup-db.ps1
pwsh -File scripts\windows\run-server.ps1
pwsh -File scripts\windows\publish-all.ps1
```

**Linux**

```bash
bash /tmp/dotnet-install.sh --channel 10.0   # siehe 12.2
bash scripts/linux/setup-db.sh
bash scripts/linux/run-server.sh
bash scripts/linux/publish-server.sh
```

**macOS**

```bash
bash scripts/macos/install-deps.sh
bash scripts/macos/setup-db.sh
bash scripts/macos/run-server.sh
bash scripts/macos/publish-server.sh
```

