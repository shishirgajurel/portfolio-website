# Turns the portfolio into plain HTML files that GitHub Pages can host.
#
# How it works: you edit your content locally with /admin (run the site in Rider),
# then run this script. It starts the site with your local database, saves every
# public page as HTML, and copies CSS, JS and uploaded images next to them.
#
# Just export (look at the result in out\github-site):
#   .\deploy\github\publish-to-github.ps1
# Export and copy into your local clone of the GitHub Pages repo, then commit:
#   .\deploy\github\publish-to-github.ps1 -RepoPath "D:\path\to\your-pages-repo"
# ...and also push to GitHub:
#   .\deploy\github\publish-to-github.ps1 -RepoPath "D:\path\to\your-pages-repo" -Push
# Extra files already in the repo that should stay online can be listed with -Keep.
param(
    [string] $BaseUrl = "https://www.shishirgajurel.com.np",
    [string] $RepoPath,
    [string[]] $Keep = @(),
    # Publish even if GitHub couldn't be reached (the repo list would then only appear via the live update).
    [switch] $AllowEmptyRepos,
    [switch] $Push,
    [int] $Port = 5123
)
$ErrorActionPreference = "Stop"
# "-Keep a.html,b.html" arrives as one string when run via "powershell -File", so split it.
$Keep = @($Keep | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$root = Resolve-Path "$PSScriptRoot\..\.."
$build = "$root\obj\export-build"
$out = "$root\out\github-site"
$local = "http://127.0.0.1:$Port"
$BaseUrl = $BaseUrl.TrimEnd('/')

Write-Host "== Building"
dotnet build "$root\PortfolioSite.csproj" -c Release -o $build -nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "== Starting the site with your local content"
$env:ASPNETCORE_URLS = $local
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Site__BaseUrl = $BaseUrl
$env:ConnectionStrings__Default = "Data Source=$root\portfolio.db"
$env:DataProtection__KeysPath = "$root\obj\export-keys"
$server = Start-Process dotnet -ArgumentList "`"$build\PortfolioSite.dll`" --contentRoot `"$root`"" `
    -WorkingDirectory $root -PassThru -WindowStyle Hidden

try {
    $up = $false
    for ($i = 0; $i -lt 30 -and -not $up; $i++) {
        try { Invoke-WebRequest "$local/robots.txt" -UseBasicParsing -TimeoutSec 2 | Out-Null; $up = $true }
        catch { Start-Sleep 1 }
    }
    if (-not $up) { throw "The site did not start." }

    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Force $out | Out-Null

    function Save-Page([string] $path, [string] $file) {
        $dest = Join-Path $out $file
        New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
        Invoke-WebRequest "$local$path" -UseBasicParsing -OutFile $dest
        Write-Host "  $path -> $file"
    }

    Write-Host "== Saving pages"
    Save-Page "/" "index.html"
    Save-Page "/robots.txt" "robots.txt"
    Save-Page "/sitemap.xml" "sitemap.xml"
    Save-Page "/Error?code=404" "404.html"

    # Every published project, taken from the sitemap.
    # GitHub Pages serves projects/slug.html at /projects/slug, so links stay the same.
    $sitemap = [xml](Get-Content "$out\sitemap.xml" -Raw -Encoding UTF8)
    foreach ($loc in $sitemap.urlset.url.loc) {
        $path = $loc.Substring($BaseUrl.Length)
        if ($path -like "/projects/*") { Save-Page $path ($path.TrimStart('/') + ".html") }
    }
}
finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "== Copying CSS, JS and images"
Get-ChildItem "$root\wwwroot" | Copy-Item -Destination $out -Recurse -Force
Remove-Item "$out\uploads\.gitkeep" -Force -ErrorAction SilentlyContinue

# Custom domain for GitHub Pages, and turn off Jekyll so every file is served as-is.
([Uri]$BaseUrl).Host | Set-Content "$out\CNAME" -NoNewline -Encoding ascii
New-Item "$out\.nojekyll" -ItemType File -Force | Out-Null

$leaks = Select-String -Path "$out\*.html", "$out\projects\*.html", "$out\*.xml", "$out\*.txt" `
    -Pattern "127\.0\.0\.1|localhost" -ErrorAction SilentlyContinue
if ($leaks) { Write-Warning "Some files still mention localhost:"; $leaks | ForEach-Object { Write-Warning $_ } }

$repoCards = (Select-String -Path "$out\index.html" -Pattern 'class="card repo"' -AllMatches).Matches.Count
if ($repoCards -eq 0 -and -not $AllowEmptyRepos) {
    throw "No GitHub repositories in the page (GitHub API unreachable or rate-limited?). Try again later, or add -AllowEmptyRepos."
}
Write-Host "GitHub repositories in the page: $repoCards"

Write-Host "Static site ready in $out" -ForegroundColor Green

if ($RepoPath) {
    if (-not (Test-Path "$RepoPath\.git")) { throw "$RepoPath is not a git repository." }
    Write-Host "== Updating $RepoPath"
    # Replace the old site, but keep git history, any GitHub settings folder and the -Keep files.
    Get-ChildItem $RepoPath -Force | Where-Object { $_.Name -notin (@(".git", ".github") + $Keep) } |
        Remove-Item -Recurse -Force
    Get-ChildItem $out -Force | Copy-Item -Destination $RepoPath -Recurse -Force

    git -C $RepoPath add -A
    git -C $RepoPath commit -m "Update portfolio site ($(Get-Date -Format 'yyyy-MM-dd'))"
    if ($Push) {
        git -C $RepoPath push
        Write-Host "Pushed. GitHub Pages updates within a minute or two." -ForegroundColor Green
    } else {
        Write-Host "Committed. Run 'git push' in $RepoPath (or add -Push) to publish." -ForegroundColor Yellow
    }
}
