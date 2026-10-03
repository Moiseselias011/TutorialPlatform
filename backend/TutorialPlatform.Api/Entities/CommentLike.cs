namespace TutorialPlatform.Api.Entities;

/// <summary>
/// Me gusta de un usuario a un comentario. Un usuario = un Me gusta por
/// comentario (índice único en la BD), la misma regla que los likes de
/// tutoriales. El contador equivalente vive en <see cref="Comment.LikeCount"/>.
/// </summary>
public class CommentLike
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
