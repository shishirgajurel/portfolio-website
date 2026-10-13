using PortfolioSite.Models;

namespace PortfolioSite.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Settings.Any())
        {
            db.Settings.Add(new SiteSettings
            {
                FullName = "Shishir Gajurel",
                Headline = "I build reliable software with Java & .NET.",
                Intro = "Software Technology Engineering student at VIA University College in Horsens, Denmark. I enjoy turning requirements into working, well-structured systems — from databases to client apps.",
                About = "I'm a third-semester Software Technology Engineering student at VIA University College in Horsens, Denmark. I moved here in 2023 and I'm looking for a student job or internship where I can learn from experienced engineers while contributing to real systems.",
                Skills = "C# / .NET, Java, MySQL, Dart, Blazor, Maven, Git & GitHub, Distributed systems, Agile teamwork",
                Email = "shishirgajurel999@gmail.com",
                Address = "Horsens, Denmark",
                Availability = "Available 15–20 hours a week during term time, and full-time in the summer."
            });
        }

        if (!db.Projects.Any())
        {
            db.Projects.Add(new Project
            {
                Title = "Library Management System (KitabKhana)",
                Slug = "library-management-system",
                Summary = "A distributed library system with multiple logic servers, a persistence layer and a Blazor client on MySQL.",
                Tags = "C#/.NET, Java, Blazor, MySQL, Git",
                Link = "https://github.com/yousseftopaji/LibraryManagementSystem",
                Details =
                    "## Overview\n" +
                    "KitabKhana is a library management system built by a small student team at VIA University College. Instead of one big application, it is split into several cooperating parts.\n" +
                    "## How it works\n" +
                    "- Logic servers that handle the library rules\n" +
                    "- A persistence layer that stores data in a MySQL database\n" +
                    "- A Blazor client that users interact with\n" +
                    "- Components in both C#/.NET and Java (Maven) inside one solution\n" +
                    "## What I learned\n" +
                    "How separate services talk to each other and stay consistent, how to work across Java and .NET, and how to collaborate in a team with Git and GitHub.",
                SortOrder = 1
            });
        }

        db.SaveChanges();
    }
}
