using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

[ApiController]
[Route("api/tutorials/{tutorialId:int}/[controller]")]
public class LikesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;

    public LikesController(AppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    // GET /api/tutorials/5/likes → quién dio like (solo ADMIN, es dato interno)
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetLikers(int tutorialId)
    {
        var likers = await _db.Likes
            .Where(l => l.TutorialId == tutorialId)
            .Include(l => l.User)
            .Select(l => new { l.UserId, l.User.Username, l.CreatedAt })
            .ToListAsync();

        return Ok(likers);
    }

    // POST /api/tutorials/5/likes → TOGGLE: da like o lo quita
    // Un usuario = un like por tutorial (índice único en la BD)
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<LikeResult>> Toggle(int tutorialId)
    {
        var tutorial = await _db.Tutorials.FindAsync(tutorialId);
        if (tutorial is null)
            return NotFound(new { message = "Tutorial no encontrado." });

        var userId = _current.Id;
        var existing = await _db.Likes
            .FirstOrDefaultAsync(l => l.TutorialId == tutorialId && l.UserId == userId);

        bool liked;

        if (existing is not null)
        {
            // Quitar Me gusta
            _db.Likes.Remove(existing);
            tutorial.LikeCount = Math.Max(0, tutorial.LikeCount - 1);
            liked = false;
        }
        else
        {
            // Dar Me gusta
            _db.Likes.Add(new Like { TutorialId = tutorialId, UserId = userId });
            tutorial.LikeCount++;
            liked = true;
        }

        // Like y contador se guardan en la MISMA transacción → siempre consistente
        await _db.SaveChangesAsync();

        return Ok(new LikeResult { Liked = liked, LikeCount = tutorial.LikeCount });
    }
}
