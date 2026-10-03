using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Las técnicas de dibujo y las estrategias de marketing se aseguran
        // SIEMPRE, aunque la base ya tenga usuarios (mismo motivo: SQLite no
        // ejecuta migraciones). Idempotentes: solo insertan las que falten.
        await AsegurarCategoriasAsync(db, Domains.Dibujo, TecnicasDibujo, "técnicas de dibujo");
        await AsegurarCategoriasAsync(db, Domains.Marketing, EstrategiasMarketing, "estrategias de marketing");

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
    /// usuario.
    /// </summary>
    private static readonly (string Nombre, string Slug)[] TecnicasDibujo =
    {
        ("Acuarela", "acuarela"),
        ("Óleo", "oleo"),
        ("Acrílico", "acrilico"),
        ("Témpera", "tempera"),
        ("Tinta", "tinta"),
        ("Pastel", "pastel"),
        ("Carbonilla", "carbonilla"),
        ("Grafito", "grafito"),
        ("Estilógrafo", "estilografo"),
        ("Plumilla", "plumilla"),
        ("Sanguina", "sanguina"),
        ("Sepia", "sepia"),
        ("Marcadores", "marcadores"),
        ("Aerógrafo", "aerografo"),
        ("Crayones", "crayones"),
        ("Bolígrafo", "boligrafo")
    };

    /// <summary>
    /// Las 16 estrategias del índice de marketing, en el orden en que las
    /// pidió el usuario. Los paréntesis de SEO y SEM van en el nombre para
    /// que el ComboBox se lea solo; el slug se queda corto.
    /// </summary>
    private static readonly (string Nombre, string Slug)[] EstrategiasMarketing =
    {
        ("Marketing de contenidos", "marketing-de-contenidos"),
        ("Inbound marketing", "inbound-marketing"),
        ("Marketing de afiliados", "marketing-de-afiliados"),
        ("Growth hacking", "growth-hacking"),
        ("Marketing de influencers", "marketing-de-influencers"),
        ("Co-branding", "co-branding"),
        ("Email marketing", "email-marketing"),
        ("Marketing de guerrilla", "marketing-de-guerrilla"),
        ("Relaciones públicas", "relaciones-publicas"),
        ("SEO (posicionamiento web)", "seo"),
        ("SEM (publicidad paga)", "sem"),
        ("Marketing experiencial", "marketing-experiencial"),
        ("Neuromarketing", "neuromarketing"),
        ("Video marketing", "video-marketing"),
        ("Marketing local", "marketing-local"),
        ("Storytelling", "storytelling")
    };

    /// <summary>
    /// Asegura que existan las categorías de un dominio. Se insertan solo las
    /// que falten: en SQL Server el reparto ya está hecho y volver a
    /// ejecutarlo no debe duplicar nada.
    /// </summary>
    private static async Task AsegurarCategoriasAsync(
        AppDbContext db,
        string dominio,
        (string Nombre, string Slug)[] items,
        string etiqueta)
    {
        var existentes = await db.Technologies
            .Where(t => t.Domain == dominio)
            .Select(t => t.Slug)
            .ToListAsync();

        var faltantes = items
            .Where(i => !existentes.Contains(i.Slug))
            .Select(i => new Technology { Name = i.Nombre, Slug = i.Slug, Domain = dominio })
            .ToArray();

        if (faltantes.Length == 0) return;

        db.Technologies.AddRange(faltantes);
        await db.SaveChangesAsync();
        Console.WriteLine($"[Seed] {faltantes.Length} {etiqueta} añadidas.");
    }
}
