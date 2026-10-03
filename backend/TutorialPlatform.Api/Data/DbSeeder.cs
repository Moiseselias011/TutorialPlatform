using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Las técnicas de dibujo se aseguran SIEMPRE, aunque la base ya tenga
        // usuarios: SQLite no ejecuta migraciones (usa EnsureCreated), así que
        // aquí es su única vía de entrada. En SQL Server llegan con la
        // migración y este método no hace nada. Idempotente: inserta solo
        // las que falten.
        await AsegurarTecnicasDibujoAsync(db);

        // Solo sembrar si no hay usuarios
        if (await db.Users.AnyAsync()) return;

        // ---------- Usuarios ----------
        // Credenciales demo: admin/admin123, maria/maria123, carlos/carlos123
        var admin = new User
        {
            Username = "admin",
            Email = "admin@tutorialplatform.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = Roles.Admin
        };

        var user1 = new User
        {
            Username = "maria",
            Email = "maria@tutorialplatform.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("maria123"),
            Role = Roles.User
        };

        var user2 = new User
        {
            Username = "carlos",
            Email = "carlos@tutorialplatform.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("carlos123"),
            Role = Roles.User
        };

        db.Users.AddRange(admin, user1, user2);

        // ---------- Tecnologías ----------
        var techs = new[]
        {
            new Technology { Name = "JavaScript", Slug = "javascript" },
            new Technology { Name = "Python",     Slug = "python" },
            new Technology { Name = "Java",       Slug = "java" },
            new Technology { Name = "Vue",        Slug = "vue" },
            new Technology { Name = "C#",         Slug = "csharp" },
            new Technology { Name = "SQL",        Slug = "sql" },
            new Technology { Name = "React",      Slug = "react" },
            new Technology { Name = "HTML/CSS",   Slug = "html-css" }
        };
        db.Technologies.AddRange(techs);

        await db.SaveChangesAsync();

        // ---------- Tutoriales de ejemplo ----------
        // Las URL apuntan a YouTube a propósito: la interfaz deriva de ellas la
        // miniatura (src/utils/youtube.js). Con enlaces de documentación no se
        // vería ninguna imagen, porque esos tutoriales no tienen imageUrl.
        var tutorials = new[]
        {
            new Tutorial
            {
                Title = "JavaScript Moderno: Guía completa ES2024",
                Description = "Aprende las novedades de JavaScript: módulos, Promises, optional chaining y más.",
                Url = "https://www.youtube.com/watch?v=PkZNo7MFNFg",
                Technology = techs[0], Author = user1,
                PublishedAt = DateTime.UtcNow.AddDays(-30), LikeCount = 0
            },
            new Tutorial
            {
                Title = "Python para principiantes",
                Description = "Curso introductorio a Python: variables, listas, funciones y estructuras de control.",
                Url = "https://www.youtube.com/watch?v=_uQrJ0TkZlc",
                Technology = techs[1], Author = user2,
                PublishedAt = DateTime.UtcNow.AddDays(-25), LikeCount = 0
            },
            new Tutorial
            {
                Title = "Spring Boot y Java REST API",
                Description = "Construye tu primera API REST con Java y Spring Boot paso a paso.",
                Url = "https://www.youtube.com/watch?v=YDRNMAJo0MA",
                Technology = techs[2], Author = admin,
                PublishedAt = DateTime.UtcNow.AddDays(-20), LikeCount = 0
            },
            new Tutorial
            {
                Title = "Vue 3 Composition API desde cero",
                Description = "Guía oficial para entender reactividad, composables y componentes en Vue 3.",
                Url = "https://www.youtube.com/watch?v=bwItFdPt-6M",
                Technology = techs[3], Author = user1,
                PublishedAt = DateTime.UtcNow.AddDays(-15), LikeCount = 0
            },
            new Tutorial
            {
                Title = "ASP.NET Core Web API Tutorial",
                Description = "Crea una API completa con C#, Entity Framework Core y SQL Server.",
                Url = "https://www.youtube.com/watch?v=38GNKtclDdE",
                Technology = techs[4], Author = admin,
                PublishedAt = DateTime.UtcNow.AddDays(-10), LikeCount = 0
            },
            new Tutorial
            {
                Title = "SQL desde cero: consultas JOIN",
                Description = "Aprende a consultar bases de datos relacionales con SELECT, WHERE y JOINs.",
                Url = "https://www.youtube.com/watch?v=FjxtntY5sO0",
                Technology = techs[5], Author = user2,
                PublishedAt = DateTime.UtcNow.AddDays(-7), LikeCount = 0
            }
        };

        db.Tutorials.AddRange(tutorials);
        await db.SaveChangesAsync();

        Console.WriteLine($"[Seed] {await db.Users.CountAsync()} usuarios, " +
                          $"{await db.Technologies.CountAsync()} tecnologías, " +
                          $"{await db.Tutorials.CountAsync()} tutoriales.");
    }

    /// <summary>
    /// Las 16 técnicas del índice de dibujo, en el orden en que las pidió el
    /// usuario. Se insertan solo si faltan: en SQL Server ya vienen de la
    /// migración y volver a ejecutarlo no debe duplicarlas.
    /// </summary>
    private static async Task AsegurarTecnicasDibujoAsync(AppDbContext db)
    {
        var tecnicas = new[]
        {
            new Technology { Name = "Acuarela",    Slug = "acuarela",    Domain = Domains.Dibujo },
            new Technology { Name = "Óleo",        Slug = "oleo",        Domain = Domains.Dibujo },
            new Technology { Name = "Acrílico",    Slug = "acrilico",    Domain = Domains.Dibujo },
            new Technology { Name = "Témpera",     Slug = "tempera",     Domain = Domains.Dibujo },
            new Technology { Name = "Tinta",       Slug = "tinta",       Domain = Domains.Dibujo },
            new Technology { Name = "Pastel",      Slug = "pastel",      Domain = Domains.Dibujo },
            new Technology { Name = "Carbonilla",  Slug = "carbonilla",  Domain = Domains.Dibujo },
            new Technology { Name = "Grafito",     Slug = "grafito",     Domain = Domains.Dibujo },
            new Technology { Name = "Estilógrafo", Slug = "estilografo", Domain = Domains.Dibujo },
            new Technology { Name = "Plumilla",    Slug = "plumilla",    Domain = Domains.Dibujo },
            new Technology { Name = "Sanguina",    Slug = "sanguina",    Domain = Domains.Dibujo },
            new Technology { Name = "Sepia",       Slug = "sepia",       Domain = Domains.Dibujo },
            new Technology { Name = "Marcadores",  Slug = "marcadores",  Domain = Domains.Dibujo },
            new Technology { Name = "Aerógrafo",   Slug = "aerografo",   Domain = Domains.Dibujo },
            new Technology { Name = "Crayones",    Slug = "crayones",    Domain = Domains.Dibujo },
            new Technology { Name = "Bolígrafo",   Slug = "boligrafo",   Domain = Domains.Dibujo }
        };

        var existentes = await db.Technologies
            .Where(t => t.Domain == Domains.Dibujo)
            .Select(t => t.Slug)
            .ToListAsync();

        var faltantes = tecnicas.Where(t => !existentes.Contains(t.Slug)).ToArray();
        if (faltantes.Length == 0) return;

        db.Technologies.AddRange(faltantes);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Seed] {faltantes.Length} técnicas de dibujo añadidas.");
    }
}
