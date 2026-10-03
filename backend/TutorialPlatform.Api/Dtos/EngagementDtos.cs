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

    /// <summary>
    /// Comentario al que se responde. Null = comentario principal.
    /// Debe pertenecer al mismo tutorial; se comprueba en el backend.
    /// </summary>
    public int? ParentCommentId { get; set; }
}

public class UpdateCommentRequest : CreateCommentRequest { }

public class CommentDto
{
    public int Id { get; set; }
    public int TutorialId { get; set; }
    public int AuthorId { get; set; }
    public string Author { get; set; } = string.Empty;

    /// <summary>Foto de perfil del autor (ruta relativa bajo /uploads/) o null.</summary>
    public string? PhotoUrl { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>Comentario padre. Null = comentario principal.</summary>
    public int? ParentCommentId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Contador de Me gusta; siempre refleja la tabla CommentLike.</summary>
    public int LikeCount { get; set; }

    /// <summary>Si el usuario autenticado ya dio Me gusta a este comentario.</summary>
    public bool LikedByMe { get; set; }

    /// <summary>Si el usuario autenticado puede editar/eliminar este comentario.</summary>
    public bool CanEdit { get; set; }
}
