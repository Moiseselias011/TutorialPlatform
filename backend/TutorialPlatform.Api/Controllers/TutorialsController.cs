using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TutorialsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ITutorialMapper _mapper;

    public TutorialsController(AppDbContext db, ICurrentUser current, ITutorialMapper mapper)
    {
        _db = db;
        _current = current;
        _mapper = mapper;
    }

    // ============================================================
    // GET /api/tutorials  → lista general con paginación/búsqueda/filtros
    // ============================================================
    [HttpGet]
    [AllowAnonymous] // cualquier visitante puede consultar las listas generales
    public async Task<ActionResult<PagedResult<TutorialDto>>> GetAll([FromQuery] TutorialQuery q)
    {
        var query = _db.Tutorials
            .Include(t => t.Technology)
            .Include(t => t.Author)
            .AsNoTracking()
            .AsQueryable();

        // ---- Filtro por dominio: el índice de programación no debe mostrar
        // ---- tutoriales de dibujo, ni el de dibujo los de programación ----
        var dominio = string.IsNullOrWhiteSpace(q.Domain)
            ? Domains.PorDefecto
            : q.Domain.Trim().ToLowerInvariant();

        if (!Domains.EsValido(dominio))
            return BadRequest(new { message = "Dominio no válido. Usa «programacion», «dibujo» o «marketing»." });

        query = query.Where(t => t.Technology.Domain == dominio);

        // ---- Filtro por tecnología (id o slug) ----
        if (!string.IsNullOrWhiteSpace(q.Technology))
        {
            if (int.TryParse(q.Technology, out var techId))
            {
                query = query.Where(t => t.TechnologyId == techId);
            }
            else
            {
                var slug = q.Technology.Trim().ToLowerInvariant();
                query = query.Where(t => t.Technology.Slug == slug);
            }
        }

        // ---- Buscador por título o descripción ----
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term));
        }

        // ---- Orden ----
        query = q.Sort?.ToLowerInvariant() switch
        {
            "likes" => query.OrderByDescending(t => t.LikeCount),
            "title" => query.OrderBy(t => t.Title),
            _ => query.OrderByDescending(t => t.PublishedAt)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)q.PageSize));

        var page = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        var dtos = await _mapper.MapManyAsync(page);

        return Ok(new PagedResult<TutorialDto>
        {
            Items = dtos,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total,
            TotalPages = totalPages
        });
    }

    // ============================================================
    // GET /api/tutorials/5
    // ============================================================
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TutorialDto>> GetById(int id)
    {
        var tutorial = await _db.Tutorials
            .AsNoTracking()
            .Include(t => t.Technology)
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tutorial is null)
            return NotFound(new { message = "Tutorial no encontrado." });

        return Ok(await _mapper.MapAsync(tutorial));
    }

    // ============================================================
    // POST /api/tutorials  → cualquier usuario autenticado
    // ============================================================
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TutorialDto>> Create(CreateTutorialRequest req)
    {
        if (!await _db.Technologies.AnyAsync(t => t.Id == req.TechnologyId))
            return BadRequest(new { message = "La tecnología indicada no existe." });

        var tutorial = new Tutorial
        {
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Url = req.Url.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim(),
            TechnologyId = req.TechnologyId,
            AuthorId = _current.Id,            // el autor se toma del token, nunca del body
            PublishedAt = DateTime.UtcNow,
            LikeCount = 0
        };

        _db.Tutorials.Add(tutorial);
        await _db.SaveChangesAsync();

        var created = await _db.Tutorials
            .AsNoTracking()
            .Include(t => t.Technology)
            .Include(t => t.Author)
            .FirstAsync(t => t.Id == tutorial.Id);

        return CreatedAtAction(nameof(GetById), new { id = tutorial.Id }, await _mapper.MapAsync(created));
    }

    // ============================================================
    // PUT /api/tutorials/5  → SOLO el autor o ADMIN
    // ============================================================
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<TutorialDto>> Update(int id, UpdateTutorialRequest req)
    {
        var tutorial = await _db.Tutorials.FindAsync(id);
        if (tutorial is null)
            return NotFound(new { message = "Tutorial no encontrado." });

        // ---- INVARIANTE: solo autor o ADMIN ----
        if (tutorial.AuthorId != _current.Id && !_current.IsAdmin)
            return Forbid();

        if (!await _db.Technologies.AnyAsync(t => t.Id == req.TechnologyId))
            return BadRequest(new { message = "La tecnología indicada no existe." });

        tutorial.Title = req.Title.Trim();
        tutorial.Description = req.Description.Trim();
        tutorial.Url = req.Url.Trim();
        tutorial.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim();
        tutorial.TechnologyId = req.TechnologyId;

        await _db.SaveChangesAsync();

        var updated = await _db.Tutorials
            .AsNoTracking()
            .Include(t => t.Technology)
            .Include(t => t.Author)
            .FirstAsync(t => t.Id == id);

        return Ok(await _mapper.MapAsync(updated));
    }

    // ============================================================
    // DELETE /api/tutorials/5  → SOLO ADMIN (INVARIANTE #1)
    // ============================================================
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var tutorial = await _db.Tutorials.FindAsync(id);
        if (tutorial is null)
            return NotFound(new { message = "Tutorial no encontrado." });

        // El backend lo garantiza además del atributo: defensa en profundidad
        if (!_current.IsAdmin)
            return Forbid();

        // Desenganchar antes las respuestas: la FK padre→hijo es Restrict, así que
        // la BD no dejaría borrar comentarios que aún referenciaran a otros.
        // Aquí se acaba borrando el hilo entero igualmente (§5.10: borrado real).
        await _db.Comments
            .Where(c => c.TutorialId == id && c.ParentCommentId != null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ParentCommentId, (int?)null));

        _db.Tutorials.Remove(tutorial); // cascade borra likes, comentarios y guardados
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
