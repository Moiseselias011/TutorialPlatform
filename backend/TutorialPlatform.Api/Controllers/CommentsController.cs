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

    // GET /api/tutorials/5/comments → lista pública, orden cronológico.
    // Se devuelven TODOS los comentarios en plano, cada uno con su
    // ParentCommentId; el frontend reconstruye el árbol de respuestas.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetAll(int tutorialId)
    {
        if (!await _db.Tutorials.AnyAsync(t => t.Id == tutorialId))
            return NotFound(new { message = "Tutorial no encontrado." });

        var comments = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.TutorialId == tutorialId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var likedIds = await MeGustaDelUsuario();

        return Ok(comments.Select(c => Map(c, likedIds.Contains(c.Id))));
    }

    // POST /api/tutorials/5/comments → usuario autenticado
    // Si ParentCommentId viene, es una respuesta a otro comentario del MISMO tutorial.
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Create(int tutorialId, CreateCommentRequest req)
    {
        if (!await _db.Tutorials.AnyAsync(t => t.Id == tutorialId))
            return NotFound(new { message = "Tutorial no encontrado." });

        var content = req.Content.Trim();
        if (content.Length == 0)
            return BadRequest(new { message = "El comentario no puede estar vacío." });

        // La autorización/validez de la respuesta se comprueba SIEMPRE aquí, en el backend.
        int? parentId = null;
        if (req.ParentCommentId is int pid)
        {
            var parent = await _db.Comments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == pid);

            if (parent is null)
                return BadRequest(new { message = "El comentario al que respondes no existe." });

            if (parent.TutorialId != tutorialId)
                return BadRequest(new { message = "No puedes responder un comentario de otro tutorial." });

            parentId = pid;
        }

        var comment = new Comment
        {
            TutorialId = tutorialId,
            AuthorId = _current.Id,   // siempre desde el token
            ParentCommentId = parentId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var created = await _db.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);

        return CreatedAtAction(nameof(GetAll), new { tutorialId }, Map(created, false));
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

        return Ok(Map(comment, await DoyMeGusta(comment.Id)));
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

        // Sus respuestas suben a comentario principal en lugar de borrarse: nadie
        // pierde su contenido (§5.3). Solo se promueven las hijas directas; la
        // descendencia conserva su hilo. La FK es Restrict, así que la BD lo exige.
        var respuestas = await _db.Comments
            .Where(c => c.ParentCommentId == comment.Id)
            .ToListAsync();

        foreach (var r in respuestas)
            r.ParentCommentId = null;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Ids de los comentarios a los que el usuario actual ya dio Me gusta.</summary>
    private async Task<HashSet<int>> MeGustaDelUsuario()
    {
        if (!_current.IsAuthenticated)
            return new HashSet<int>();

        var ids = await _db.CommentLikes
            .Where(cl => cl.UserId == _current.Id)
            .Select(cl => cl.CommentId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    private Task<bool> DoyMeGusta(int commentId)
    {
        if (!_current.IsAuthenticated)
            return Task.FromResult(false);

        return _db.CommentLikes.AnyAsync(
            cl => cl.CommentId == commentId && cl.UserId == _current.Id);
    }

    private CommentDto Map(Comment c, bool likedByMe) => new()
    {
        Id = c.Id,
        TutorialId = c.TutorialId,
        AuthorId = c.AuthorId,
        Author = c.Author?.Username ?? string.Empty,
        PhotoUrl = c.Author?.PhotoUrl,
        Content = c.Content,
        ParentCommentId = c.ParentCommentId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        LikeCount = c.LikeCount,
        LikedByMe = likedByMe,
        CanEdit = _current.IsAuthenticated && (c.AuthorId == _current.Id || _current.IsAdmin)
    };
}
