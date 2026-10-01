using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class Comment
{
    public int Id { get; set; }

    public int TutorialId { get; set; }
    public Tutorial Tutorial { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
