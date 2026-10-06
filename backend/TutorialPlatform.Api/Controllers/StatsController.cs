using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Controllers;

/// <summary>
/// Contador de visitas → /api/stats
///
/// - POST /api/stats/visit → registra la visita. Anónimo: la mayoría de los
///   visitantes no inician sesión, y que no la tengan no puede impedir contarlos.
/// - GET  /api/stats → resumen. SOLO ADMIN (§2.2 «acciones administrativas» y
///   §5.8 «toda autorización se valida en el backend»): un USER recibe 403.
///
/// El recuento mide a los demás: la sesión de ADMIN no se registra, para que
/// abrir la propia página de estadísticas no inflen las cifras que se miran.
/// </summary>
[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    /// <summary>
    /// Sal fija. Impide (a) reconstruir la IP a partir del hash y (b) que dos
    /// instalaciones distintas mezclen hashes incompatibles entre sí.
    /// </summary>
    private const string Sal = "TutorialPlatform|visitas|v1";

    /// <summary>Rutas que jamás son una visita a una página de la SPA.</summary>
    private static readonly string[] RutasDeServicio = { "/api/", "/assets/", "/uploads/" };

    private readonly AppDbContext _db;

    public StatsController(AppDbContext db) => _db = db;

    // ---------- Registrar ----------
    // POST /api/stats/visit → 204. No requiere sesión.
    [HttpPost("visit")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVisitRequest req)
    {
        // Un fallo aquí jamás debe romper la navegación, pero mejor no
        // ensuciar las cifras con rastreadores ni previsualizadores de enlaces.
        if (EsRastreador(HttpContext.Request.Headers.UserAgent))
            return NoContent();

        var ruta = NormalizarRuta(req.Path);
        if (RutasDeServicio.Any(r => ruta.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            return NoContent();

        _db.Visits.Add(new Visit
        {
            OccurredAt = DateTime.Now,
            Path = ruta,
            VisitorHash = HashearIp(),
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Resumen ----------
    // GET /api/stats → solo ADMIN
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Resumen()
    {
        // Tres columnas cortas de todas las filas. La agregación se hace en
        // memoria a propósito: un GroupBy con un Count() distinto anidado no
        // se traduce de forma fiable a SQL Server y es justo el cálculo que
        // hacen las dos consultas de abajo. El volumen de este sitio (miles
        // de filas) no justifica ese riesgo.
        var filas = await _db.Visits
            .AsNoTracking()
            .Select(v => new { v.OccurredAt, v.Path, v.VisitorHash })
            .ToListAsync();

        var hoy = DateTime.Today;

        var resumen = new StatsResumenDto
        {
            VisitasTotales = filas.Count,
            VisitantesUnicos = filas.Select(v => v.VisitorHash).Distinct().Count(),
            VisitasHoy = filas.Count(v => v.OccurredAt >= hoy),
            PorPagina = filas
                .GroupBy(v => v.Path)
                .Select(g => new PaginaVisitaDto
                {
                    Ruta = g.Key,
                    Visitas = g.Count(),
                    Unicos = g.Select(v => v.VisitorHash).Distinct().Count(),
                })
                .OrderByDescending(p => p.Visitas)
                .ThenBy(p => p.Ruta)
                .ToList(),
            // Últimos 30 días (incluido el de hoy): con más no cabe en pantalla.
            PorDia = filas
                .Where(v => v.OccurredAt >= hoy.AddDays(-29))
                .GroupBy(v => v.OccurredAt.Date)
                .Select(g => new DiaVisitaDto
                {
                    Fecha = g.Key,
                    Visitas = g.Count(),
                    Unicos = g.Select(v => v.VisitorHash).Distinct().Count(),
                })
                .OrderBy(d => d.Fecha)
                .ToList(),
        };

        resumen.PaginasDistintas = resumen.PorPagina.Count;
        return Ok(resumen);
    }

    // ---------- Auxiliares ----------

    /// <summary>
    /// Hash de la IP. Las IPv4 llegan a veces envueltas en IPv6
    /// (::ffff:1.2.3.4): sin normalizarlo, el mismo visitante contaría dos.
    /// </summary>
    private string HashearIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            ip = ip[7..];

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Sal + "|" + ip));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Rutas limpias y comparables: sin cadena de consulta, sin hash, con la
    /// barra inicial y con los IDs numéricos colapsados a «{id}».
    /// </summary>
    private static string NormalizarRuta(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return "/";

        var corte = ruta.IndexOfAny(new[] { '?', '#' });
        if (corte >= 0) ruta = ruta[..corte];

        ruta = ruta.Trim();
        if (!ruta.StartsWith('/')) ruta = "/" + ruta;

        var partes = ruta.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < partes.Length; i++)
            if (int.TryParse(partes[i], out _)) partes[i] = "{id}";

        ruta = "/" + string.Join('/', partes);
        return ruta.Length > 200 ? ruta[..200] : ruta;
    }

    /// <summary>Los rastreadores y los previsualizadores de enlaces no son gente.</summary>
    private static bool EsRastreador(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return false;

        var ua = userAgent.ToLowerInvariant();
        return ua.Contains("bot") || ua.Contains("crawler") || ua.Contains("spider")
            || ua.Contains("slurp") || ua.Contains("preview") || ua.Contains("lighthouse");
    }
}
