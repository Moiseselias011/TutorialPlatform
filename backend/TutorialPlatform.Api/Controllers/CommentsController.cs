using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

[ApiController]
[Route("api/tutorials/{tutorialId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;

    public CommentsController(AppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    // GET /api/tutorials/5/comments → lista pública, orden cronológico
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetAll(int tutorialId)
    {
        if (!await _db.Tutorials.AnyAsync(t => t.Id == tutorialId))
            return NotFound(new { message = "Tutorial no encontrado." });

        var comments = await _db.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.TutorialId == tutorialId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(Map));
    }

    // POST /api/tutorials/5/comments → usuario autenticado
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Create(int tutorialId, CreateCommentRequest req)
    {
        if (!await _db.Tutorials.AnyAsync(t => t.Id == tutorialId))
            return NotFound(new { message = "Tutorial no encontrado." });

        var content = req.Content.Trim();
        if (content.Length == 0)
            return BadRequest(new { message = "El comentario no puede estar vacío." });

        var comment = new Comment
        {
            TutorialId = tutorialId,
            AuthorId = _current.Id,   // siempre desde el token
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var created = await _db.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);

        return CreatedAtAction(nameof(GetAll), new { tutorialId }, Map(created));
    }

    // PUT /api/tutorials/5/comments/3 → SOLO el autor o ADMIN
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Update(int tutorialId, int id, UpdateCommentRequest req)
    {
        var comment = await _db.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id && c.TutorialId == tutorialId);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        if (comment.AuthorId != _current.Id && !_current.IsAdmin)
            return Forbid();

        var content = req.Content.Trim();
        if (content.Length == 0)
            return BadRequest(new { message = "El comentario no puede estar vacío." });

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(Map(comment));
    }

    // DELETE /api/tutorials/5/comments/3 → autor o ADMIN
    // (los tutoriales solo los borra ADMIN; los comentarios sí puede borrarlos su autor)
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int tutorialId, int id)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == id && c.TutorialId == tutorialId);

        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        if (comment.AuthorId != _current.Id && !_current.IsAdmin)
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private CommentDto Map(Comment c) => new()
    {
        Id = c.Id,
        TutorialId = c.TutorialId,
        AuthorId = c.AuthorId,
        Author = c.Author?.Username ?? string.Empty,
        Content = c.Content,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        CanEdit = _current.IsAuthenticated && (c.AuthorId == _current.Id || _current.IsAdmin)
    };
}
