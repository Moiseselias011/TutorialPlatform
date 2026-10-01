using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

// ---------- Likes ----------

public class LikeResult
{
    public bool Liked { get; set; }
    public int LikeCount { get; set; }
}

// ---------- Comentarios ----------

public class CreateCommentRequest
{
    [Required, MaxLength(1000)]
    [MinLength(1, ErrorMessage = "El comentario no puede estar vacío.")]
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentRequest : CreateCommentRequest { }

public class CommentDto
{
    public int Id { get; set; }
    public int TutorialId { get; set; }
    public int AuthorId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Si el usuario autenticado puede editar/eliminar este comentario.</summary>
    public bool CanEdit { get; set; }
}
