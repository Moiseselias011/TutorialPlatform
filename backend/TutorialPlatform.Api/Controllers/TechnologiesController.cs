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

    // GET /api/technologies  → lista general + conteo de tutoriales
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Technologies
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

        var tech = new Technology { Name = name, Slug = slug, ImageUrl = req.ImageUrl };
        _db.Technologies.Add(tech);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tech.Id }, new { tech.Id, tech.Name, tech.Slug, tech.ImageUrl });
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
}
