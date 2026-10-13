# Run on your main PC. Builds the site for Windows and creates PortfolioLaptop.zip
# to copy to the laptop (USB stick, OneDrive, etc.).
#   .\deploy\windows\make-laptop-package.ps1
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."
$stage = "$root\obj\laptop-package"
$zip = "$root\out\PortfolioLaptop.zip"

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage, "$root\out" | Out-Null

dotnet publish "$root\PortfolioSite.csproj" -c Release -r win-x64 --self-contained true -o "$stage\app"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Never ship local databases or uploads.
Get-ChildItem "$stage\app" -Filter "*.db*" | Remove-Item -Force
Remove-Item "$stage\app\wwwroot\uploads\*" -Force -ErrorAction SilentlyContinue

Copy-Item "$PSScriptRoot\install-on-laptop.ps1" $stage
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive "$stage\*" $zip
Write-Host "Created $zip"
