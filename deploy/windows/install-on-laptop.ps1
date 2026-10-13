# Installs or updates the portfolio site as a Windows service on the laptop server.
# Run in PowerShell **as Administrator** from the unzipped package folder:
#   powershell -ExecutionPolicy Bypass -File .\install-on-laptop.ps1
$ErrorActionPreference = "Stop"
$serviceName = "PortfolioSite"
$root = "C:\Portfolio"
$app = "$root\app"
$data = "$root\data"
$pkg = $PSScriptRoot

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw "Please run this script in PowerShell as Administrator." }

Write-Host "== Preparing folders"
New-Item -ItemType Directory -Force "$data\uploads", "$data\keys" | Out-Null

$existing = Get-Service $serviceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne "Stopped") {
    Write-Host "== Stopping the running site"
    Stop-Service $serviceName
    Start-Sleep 2
}

Write-Host "== Copying the new version"
if (Test-Path $app) { Remove-Item $app -Recurse -Force }
Copy-Item "$pkg\app" $app -Recurse
# Uploaded images live in the data folder so an update never deletes them.
if (Test-Path "$app\wwwroot\uploads") { Remove-Item "$app\wwwroot\uploads" -Recurse -Force }
cmd /c mklink /J "$app\wwwroot\uploads" "$data\uploads" | Out-Null

$secrets = "$data\secrets.json"
if (-not (Test-Path $secrets)) {
    Write-Host ""
    Write-Host "== Admin login for /admin (stored only on this laptop)"
    $email = Read-Host "Admin email"
    $pw = Read-Host "Admin password (min. 14 characters)" -AsSecureString
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pw))
    @{ Admin = @{ Email = $email; Password = $plain } } | ConvertTo-Json | Set-Content $secrets -Encoding UTF8
    $plain = $null
}

# Only the service account and administrators may read the data folder (it holds the password).
icacls $data /inheritance:r /grant:r "Administrators:(OI)(CI)F" "SYSTEM:(OI)(CI)F" "NT AUTHORITY\LocalService:(OI)(CI)M" /T /Q | Out-Null
icacls $app /grant "NT AUTHORITY\LocalService:(OI)(CI)RX" /T /Q | Out-Null

if (-not $existing) {
    Write-Host "== Creating the Windows service"
    New-Service -Name $serviceName -BinaryPathName "`"$app\PortfolioSite.exe`"" `
        -DisplayName "Portfolio website" -StartupType Automatic | Out-Null
    sc.exe config $serviceName obj= "NT AUTHORITY\LocalService" | Out-Null
    # Restart automatically if it ever crashes.
    sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/5000/restart/30000 | Out-Null
}

# Settings for the service (no secrets here; those are in secrets.json).
$envVars = @(
    "ASPNETCORE_URLS=http://127.0.0.1:5000",
    "ASPNETCORE_ENVIRONMENT=Production",
    "ConnectionStrings__Default=Data Source=$data\portfolio.db",
    "DataProtection__KeysPath=$data\keys",
    "SecretsFile=$secrets"
)
Set-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName" -Name Environment -Value $envVars -Type MultiString

Write-Host "== Starting the site"
Start-Service $serviceName
Start-Sleep 4
try {
    Invoke-WebRequest "http://127.0.0.1:5000/robots.txt" -UseBasicParsing -TimeoutSec 10 | Out-Null
    Write-Host "Site is running at http://localhost:5000" -ForegroundColor Green
} catch {
    Write-Host "The site did not respond. Check Event Viewer > Windows Logs > Application." -ForegroundColor Red
    exit 1
}
