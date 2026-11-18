# Run elevated (Administrator)
# Opens TCP 5101 for the Enova API (adjust -RemoteAddress for tighter scope)

param(
    [string]$Port = "5101",
    [string]$RemoteAddress = "LocalSubnet"   # or specific CIDR e.g. 10.0.0.0/8
)

New-NetFirewallRule `
    -DisplayName "eNoVa_tor API ($Port)" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort $Port `
    -Action Allow `
    -RemoteAddress $RemoteAddress `
    -Profile Domain,Private

Write-Host "Firewall rule created for TCP $Port from $RemoteAddress (Domain/Private)."
