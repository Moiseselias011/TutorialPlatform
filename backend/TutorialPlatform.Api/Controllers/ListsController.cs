using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

/// <summary>
/// Listas personales del usuario autenticado.
/// INVARIANTE: cada usuario solo puede ver y administrar LAS SUYAS.
/// </summary>
[ApiController]
[Route("api/lists")]
[Authorize]
public class ListsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ITutorialMapper _mapper;

    public ListsController(AppDbContext db, ICurrentUser current, ITutorialMapper mapper)
    {
        _db = db;
        _current = current;
        _mapper = mapper;
    }

    // GET /api/lists → solo las del usuario actual
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonalListDto>>> GetMine()
    {
        var lists = await _db.PersonalLists
            .AsNoTracking()
            .Where(l => l.OwnerId == _current.Id)
            .OrderBy(l => l.Name)
            .Select(l => new PersonalListDto
            {
                Id = l.Id,
                Name = l.Name,
                OwnerId = l.OwnerId,
                CreatedAt = l.CreatedAt,
                TutorialCount = l.SavedTutorials.Count()
            })
            .ToListAsync();

        return Ok(lists);
    }

    // GET /api/lists/5 → detalle PAGINADO con buscador (solo dueño)
    // La lista no tiene tope de tutoriales, así que la paginación vive en el
    // SERVIDOR —igual que en GET /api/tutorials— en lugar de descargarla entera
    // y recortarla en el navegador.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonalListDetailDto>> GetById(int id, [FromQuery] PersonalListQuery q)
    {
        var list = await _db.PersonalLists
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (list is null)
            return NotFound(new { message = "Lista no encontrada." });

        if (list.OwnerId != _current.Id)
            return Forbid(); // las listas personales son privadas

        var saved = _db.SavedTutorials
            .AsNoTracking()
            .Where(s => s.PersonalListId == id);

        // Total REAL de la lista: se cuenta ANTES del filtro, es el de la cabecera
        var totalEnLista = await saved.CountAsync();

        // ---- Buscador dentro de la lista (título, descripción, tecnología, autor) ----
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLowerInvariant();
            saved = saved.Where(s =>
                s.Tutorial.Title.ToLower().Contains(term) ||
                s.Tutorial.Description.ToLower().Contains(term) ||
                s.Tutorial.Technology.Name.ToLower().Contains(term) ||
                s.Tutorial.Author.Username.ToLower().Contains(term));
        }

        var total = await saved.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)q.PageSize));

        // "Más reciente guardado primero" + recorte de la página en una sola consulta
        var orderedIds = await saved
            .OrderByDescending(s => s.SavedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(s => s.TutorialId)
            .ToListAsync();

        // EF Core no permite Include tras un Select: obtenemos los ids primero
        var tutorials = await _db.Tutorials
            .AsNoTracking()
            .Include(t => t.Technology)
            .Include(t => t.Author)
            .Where(t => orderedIds.Contains(t.Id))
            .ToListAsync();

        // Restaurar el orden "más reciente guardado primero"
        var byId = tutorials.ToDictionary(t => t.Id);
        var ordered = orderedIds
            .Where(i => byId.ContainsKey(i))
            .Select(i => byId[i])
            .ToList();

        return Ok(new PersonalListDetailDto
        {
            Id = list.Id,
            Name = list.Name,
            OwnerId = list.OwnerId,
            CreatedAt = list.CreatedAt,
            TutorialCount = totalEnLista,
            Tutorials = new PagedResult<TutorialDto>
            {
                Items = await _mapper.MapManyAsync(ordered),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = total,
                TotalPages = totalPages
            }
        });
    }

    // POST /api/lists → crear lista personal
    [HttpPost]
    public async Task<ActionResult<PersonalListDto>> Create(CreateListRequest req)
    {
        var name = req.Name.Trim();
        if (name.Length < 2)
            return BadRequest(new { message = "El nombre debe tener al menos 2 caracteres." });

        if (await _db.PersonalLists.AnyAsync(l => l.OwnerId == _current.Id && l.Name == name))
            return Conflict(new { message = "Ya tienes una lista con ese nombre." });

        var list = new PersonalList
        {
            Name = name,
            OwnerId = _current.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.PersonalLists.Add(list);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = list.Id }, new PersonalListDto
        {
            Id = list.Id,
            Name = list.Name,
            OwnerId = list.OwnerId,
            CreatedAt = list.CreatedAt,
            TutorialCount = 0
        });
    }

    // PUT /api/lists/5 → renombrar (solo dueño)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PersonalListDto>> Update(int id, UpdateListRequest req)
    {
        var list = await _db.PersonalLists.FindAsync(id);
        if (list is null)
            return NotFound(new { message = "Lista no encontrada." });

        if (list.OwnerId != _current.Id)
            return Forbid();

        var name = req.Name.Trim();
        if (name.Length < 2)
            return BadRequest(new { message = "El nombre debe tener al menos 2 caracteres." });

        if (await _db.PersonalLists.AnyAsync(l =>
                l.OwnerId == _current.Id && l.Name == name && l.Id != id))
            return Conflict(new { message = "Ya tienes una lista con ese nombre." });

        list.Name = name;
        await _db.SaveChangesAsync();

        var count = await _db.SavedTutorials.CountAsync(s => s.PersonalListId == id);

        return Ok(new PersonalListDto
        {
            Id = list.Id,
            Name = list.Name,
            OwnerId = list.OwnerId,
            CreatedAt = list.CreatedAt,
            TutorialCount = count
        });
    }

    // DELETE /api/lists/5 → borra LA LISTA (no el tutorial de la plataforma)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var list = await _db.PersonalLists.FindAsync(id);
        if (list is null)
            return NotFound(new { message = "Lista no encontrada." });

        if (list.OwnerId != _current.Id)
            return Forbid();

        // Cascade borra solo las filas de SavedTutorials; los tutoriales permanecen
        _db.PersonalLists.Remove(list);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // GUARDAR / QUITAR tutorial  (modal con buscador + ComboBox)
    // ============================================================

    // POST /api/lists/5/save  { tutorialId: 10 }
    [HttpPost("{id:int}/save")]
    public async Task<ActionResult<SaveResult>> Save(int id, [FromBody] SaveTutorialRequest req)
    {
        var list = await _db.PersonalLists.FindAsync(id);
        if (list is null)
            return NotFound(new { message = "Lista no encontrada." });

        if (list.OwnerId != _current.Id)
            return Forbid();

        if (!await _db.Tutorials.AnyAsync(t => t.Id == req.TutorialId))
            return NotFound(new { message = "Tutorial no encontrado." });

        var exists = await _db.SavedTutorials.AnyAsync(s =>
            s.PersonalListId == id && s.TutorialId == req.TutorialId);

        if (exists)
            return Ok(new SaveResult
            {
                Saved = true,
                PersonalListId = id,
                ListName = list.Name,
                TutorialCount = await _db.SavedTutorials.CountAsync(s => s.PersonalListId == id),
                Message = "El tutorial ya estaba en esta lista."
            });

        _db.SavedTutorials.Add(new SavedTutorial
        {
            PersonalListId = id,
            TutorialId = req.TutorialId,
            SavedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new SaveResult
        {
            Saved = true,
            PersonalListId = id,
            ListName = list.Name,
            TutorialCount = await _db.SavedTutorials.CountAsync(s => s.PersonalListId == id),
            Message = $"Guardado en \"{list.Name}\"."
        });
    }

    // DELETE /api/lists/5/save/10 → quitar de la lista (sigue existiendo en la plataforma)
    [HttpDelete("{id:int}/save/{tutorialId:int}")]
    public async Task<ActionResult<SaveResult>> Unsave(int id, int tutorialId)
    {
        var list = await _db.PersonalLists.FindAsync(id);
        if (list is null)
            return NotFound(new { message = "Lista no encontrada." });

        if (list.OwnerId != _current.Id)
            return Forbid();

        var saved = await _db.SavedTutorials
            .FirstOrDefaultAsync(s => s.PersonalListId == id && s.TutorialId == tutorialId);

        if (saved is null)
            return NotFound(new { message = "Este tutorial no está en la lista." });

        _db.SavedTutorials.Remove(saved);
        await _db.SaveChangesAsync();

        return Ok(new SaveResult
        {
            Saved = false,
            PersonalListId = id,
            ListName = list.Name,
            TutorialCount = await _db.SavedTutorials.CountAsync(s => s.PersonalListId == id),
            Message = $"Quitado de \"{list.Name}\"."
        });
    }

    // ============================================================
    // Buscador del modal: mis listas filtradas (para elegir destino)
    // GET /api/lists/search?q=favoritos
    // ============================================================
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        var query = _db.PersonalLists
            .AsNoTracking()
            .Where(l => l.OwnerId == _current.Id);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(l => l.Name.ToLower().Contains(term));
        }

        var result = await query
            .OrderBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Name,
                TutorialCount = l.SavedTutorials.Count()
            })
            .ToListAsync();

        return Ok(result);
    }
}
