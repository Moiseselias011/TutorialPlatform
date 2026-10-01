using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Services;

public interface ITutorialMapper
{
    Task<TutorialDto> MapAsync(Tutorial t);
    Task<List<TutorialDto>> MapManyAsync(IReadOnlyList<Tutorial> tutorials);
}

/// <summary>
/// Mapea Tutorial → TutorialDto calculando los flags del usuario autenticado
/// (likedByMe, savedByMe, canEdit, canDelete) sin consultas N+1.
/// </summary>
public class TutorialMapper : ITutorialMapper
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _current;

    public TutorialMapper(AppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<TutorialDto> MapAsync(Tutorial t)
        => (await MapManyAsync(new[] { t })).First();

    public async Task<List<TutorialDto>> MapManyAsync(IReadOnlyList<Tutorial> tutorials)
    {
        if (tutorials.Count == 0) return new List<TutorialDto>();

        var ids = tutorials.Select(t => t.Id).ToList();
        var userId = _current.Id;
        var hasSession = _current.IsAuthenticated;

        // 1 consulta: conteo de comentarios por tutorial
        var commentCounts = await _db.Comments
            .Where(c => ids.Contains(c.TutorialId))
            .GroupBy(c => c.TutorialId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        // 1 consulta: likes del usuario actual sobre esta página
        var myLikes = hasSession
            ? (await _db.Likes
                .Where(l => l.UserId == userId && ids.Contains(l.TutorialId))
                .Select(l => l.TutorialId)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        // 1 consulta: tutoriales guardados por el usuario actual
        var mySaved = hasSession
            ? (await _db.SavedTutorials
                .Where(s => s.PersonalList.OwnerId == userId && ids.Contains(s.TutorialId))
                .Select(s => s.TutorialId)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        return tutorials.Select(t => new TutorialDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Url = t.Url,
            ImageUrl = t.ImageUrl,
            TechnologyId = t.TechnologyId,
            Technology = t.Technology?.Name ?? string.Empty,
            AuthorId = t.AuthorId,
            Author = t.Author?.Username ?? string.Empty,
            PublishedAt = t.PublishedAt,
            LikeCount = t.LikeCount,
            CommentCount = commentCounts.GetValueOrDefault(t.Id, 0),
            LikedByMe = hasSession ? myLikes.Contains(t.Id) : null,
            SavedByMe = hasSession && mySaved.Contains(t.Id),
            CanEdit = hasSession && (t.AuthorId == userId || _current.IsAdmin),
            CanDelete = hasSession && _current.IsAdmin
        }).ToList();
    }
}
