using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TechnologiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TechnologiesController(AppDbContext db) => _db = db;

    // GET /api/technologies?domain=programacion|dibujo
    // → lista de un dominio + conteo de tutoriales. Sin parámetro devuelve
    //   el de programación, para que quien no pida nada siga viendo lo de antes.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? domain)
    {
        var dominio = string.IsNullOrWhiteSpace(domain)
            ? Domains.PorDefecto
            : domain.Trim().ToLowerInvariant();

        if (!Domains.EsValido(dominio))
            return BadRequest(new { message = "Dominio no válido. Usa «programacion», «dibujo» o «marketing»." });

        var items = await _db.Technologies
            .Where(t => t.Domain == dominio)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.ImageUrl,
                TutorialCount = t.Tutorials.Count()
            })
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/technologies/5  → detalle + tutoriales de esa tecnología
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var tech = await _db.Technologies
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.ImageUrl,
                TutorialCount = t.Tutorials.Count()
            })
            .FirstOrDefaultAsync();

        return tech is null ? NotFound(new { message = "Tecnología no encontrada." }) : Ok(tech);
    }

    // POST /api/technologies  → solo ADMIN
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateTechnologyRequest req)
    {
        var name = req.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? name.ToLowerInvariant().Replace(" ", "-").Replace("#", "sharp").Replace("+", "plus")
            : req.Slug.Trim().ToLowerInvariant();

        if (await _db.Technologies.AnyAsync(t => t.Name == name || t.Slug == slug))
            return Conflict(new { message = "Esa tecnología ya existe." });

        var dominio = string.IsNullOrWhiteSpace(req.Domain)
            ? Domains.PorDefecto
            : req.Domain.Trim().ToLowerInvariant();

        if (!Domains.EsValido(dominio))
            return BadRequest(new { message = "Dominio no válido. Usa «programacion», «dibujo» o «marketing»." });

        var tech = new Technology { Name = name, Slug = slug, ImageUrl = req.ImageUrl, Domain = dominio };
        _db.Technologies.Add(tech);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tech.Id }, new { tech.Id, tech.Name, tech.Slug, tech.ImageUrl, tech.Domain });
    }

    // DELETE /api/technologies/5 → solo ADMIN
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var tech = await _db.Technologies.FindAsync(id);
        if (tech is null) return NotFound(new { message = "Tecnología no encontrada." });

        if (await _db.Tutorials.AnyAsync(t => t.TechnologyId == id))
            return Conflict(new { message = "No se puede eliminar: hay tutoriales asociados." });

        _db.Technologies.Remove(tech);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateTechnologyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>«programacion» (por defecto) o «dibujo».</summary>
    public string? Domain { get; set; }
}
