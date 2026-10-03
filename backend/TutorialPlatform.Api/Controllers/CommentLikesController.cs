using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

/// <summary>
/// Me gusta de los COMENTARIOS → /api/comments/{id}/likes
/// Mismo patrón y misma regla que los likes de tutoriales: un usuario = un
/// Me gusta por comentario, y el contador siempre refleja la tabla CommentLike.
/// </summary>
[ApiController]
[Route("api/comments/{commentId:int}/likes")]
public class CommentLikesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;

    public CommentLikesController(AppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    // GET /api/comments/3/likes → quién dio Me gusta (solo ADMIN, es dato interno)
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetLikers(int commentId)
    {
        var likers = await _db.CommentLikes
            .Where(cl => cl.CommentId == commentId)
            .Include(cl => cl.User)
            .Select(cl => new { cl.UserId, cl.User.Username, cl.CreatedAt })
            .ToListAsync();

        return Ok(likers);
    }

    // POST /api/comments/3/likes → TOGGLE: da Me gusta o lo quita
    // Un usuario = un Me gusta por comentario (índice único en la BD)
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<LikeResult>> Toggle(int commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment is null)
            return NotFound(new { message = "Comentario no encontrado." });

        var userId = _current.Id;
        var existing = await _db.CommentLikes
            .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

        bool liked;

        if (existing is not null)
        {
            // Quitar Me gusta
            _db.CommentLikes.Remove(existing);
            comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
            liked = false;
        }
        else
        {
            // Dar Me gusta
            _db.CommentLikes.Add(new CommentLike { CommentId = commentId, UserId = userId });
            comment.LikeCount++;
            liked = true;
        }

        // Like y contador se guardan en la MISMA transacción → siempre consistente
        await _db.SaveChangesAsync();

        return Ok(new LikeResult { Liked = liked, LikeCount = comment.LikeCount });
    }
}
