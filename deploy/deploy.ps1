# Builds the site for Linux and uploads it to the Oracle VM.
# First time:   .\deploy\deploy.ps1 -Server 1.2.3.4 -FirstTime
# Updates:      .\deploy\deploy.ps1 -Server 1.2.3.4
param(
    [Parameter(Mandatory)] [string] $Server,
    [string] $User = "ubuntu",
    [string] $Key = "$env:USERPROFILE\.ssh\oracle_portfolio",
    # linux-arm64 for the Ampere A1 VM, linux-x64 for the AMD micro VM.
    [string] $Runtime = "linux-arm64",
    [switch] $FirstTime
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "obj\publish-$Runtime"
$tgz = Join-Path $root "obj\portfolio-release.tgz"
$target = "$User@$Server"
$sshOpts = @("-i", $Key, "-o", "StrictHostKeyChecking=accept-new")

Write-Host "== Building for $Runtime"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish "$root\PortfolioSite.csproj" -c Release -r $Runtime --self-contained true -o $out
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Never ship local databases or uploads to the server.
Get-ChildItem $out -Filter "*.db*" | Remove-Item -Force
Remove-Item "$out\wwwroot\uploads\*" -Force -ErrorAction SilentlyContinue

tar -czf $tgz -C $out .
if ($LASTEXITCODE -ne 0) { throw "Packing failed" }

Write-Host "== Uploading to $Server"
$files = @($tgz, "$PSScriptRoot\install-release.sh")
if ($FirstTime) { $files += "$PSScriptRoot\setup-server.sh", "$PSScriptRoot\portfolio.service" }
scp @sshOpts @files "${target}:~/"
if ($LASTEXITCODE -ne 0) { throw "Upload failed" }

if ($FirstTime) {
    Write-Host "== First-time server setup (you will be asked for the admin email and password)"
    ssh -t @sshOpts $target "bash ~/setup-server.sh"
    if ($LASTEXITCODE -ne 0) { throw "Server setup failed" }
}

Write-Host "== Installing new version"
ssh @sshOpts $target "bash ~/install-release.sh"
if ($LASTEXITCODE -ne 0) { throw "Install failed" }
Write-Host "Done."
