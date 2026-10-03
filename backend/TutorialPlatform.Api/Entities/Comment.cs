using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class Comment
{
    public int Id { get; set; }

    public int TutorialId { get; set; }
    public Tutorial Tutorial { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    /// <summary>
    /// Comentario al que responde este. Null = comentario principal.
    /// No hay límite de profundidad: una respuesta puede responderse a su vez.
    /// </summary>
    public int? ParentCommentId { get; set; }
    public Comment? Parent { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Contador de Me gusta. Siempre refleja la tabla <see cref="CommentLike"/>,
    /// igual que el <c>likeCount</c> de los tutoriales refleja la tabla Like (§5.9).
    /// </summary>
    public int LikeCount { get; set; }

    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
