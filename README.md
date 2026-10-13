# Portfolio website — ASP.NET Core Razor Pages

Source code of my personal website, **[shishirgajurel.com.np](https://www.shishirgajurel.com.np)**.
It's a small content-managed site: I edit my profile and projects in a private admin panel, and the public pages are generated from a database.

## Features
- **Public site**: hero, about, skills, projects and contact, with light/dark mode
- **Project pages** with their own SEO-friendly address (`/projects/<slug>`); details are written with simple `## Heading` and `- bullet` syntax
- **Admin panel** at `/admin` (not linked from the site): edit profile photo, texts, links and availability; add, edit and delete projects with images
- **Login** with cookie authentication, constant-time password comparison and **rate limiting** against password guessing
- **Secure uploads**: only JPG/PNG/WebP up to 5 MB
- **SEO**: `sitemap.xml`, `robots.txt`, canonical and Open Graph tags, JSON-LD structured data
- **Security headers**: Content-Security-Policy, X-Frame-Options, Referrer-Policy and more
- **Static export** to GitHub Pages, plus scripts for running it on a Linux VM or a Windows machine

## Tech
C# · .NET 10 · ASP.NET Core Razor Pages · Entity Framework Core · SQLite · HTML/CSS/JS

## Project structure
| Folder | Purpose |
|---|---|
| `Pages/` | Public pages, project details and the `/admin` area |
| `Data/` | EF Core `AppDbContext` and seed data |
| `Models/` | `Project` and `SiteSettings` |
| `Services/` | Image storage, slug and content helpers, public URL helper |
| `deploy/github/` | Static export to GitHub Pages |
| `deploy/` | Optional scripts for a Linux VM or a Windows laptop server |

## Run it locally
Requires the **.NET 10 SDK**.
```bash
dotnet user-secrets set "Admin:Email" "you@example.com"
dotnet user-secrets set "Admin:Password" "a-long-unique-password"
dotnet run
```
Open the URL shown in the console. The admin panel is at `/admin`.

## Publish to GitHub Pages
GitHub Pages can't run .NET, so the site is exported as plain HTML:
1. Edit content at `/admin` on your machine.
2. Run in PowerShell:
   ```powershell
   .\deploy\github\publish-to-github.ps1 -RepoPath "<local clone of the Pages repo>" -Push
   ```
   `-Keep a.html,b.html` keeps other files that already live in the Pages repo. Without `-RepoPath` the files are only written to `out\github-site`.

## Running it as a server (optional)
- **Linux VM** (e.g. Oracle Cloud): `deploy\deploy.ps1 -Server <ip> -FirstTime`
- **Windows machine**: `deploy\windows\make-laptop-package.ps1`, then `install-on-laptop.ps1` on that machine

Set `Admin__Email`, `Admin__Password` and `ConnectionStrings__Default` on the server, and keep the database and uploads on a persistent disk.
