using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class Tutorial
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Contador de Me gusta, sincronizado con la tabla Like.</summary>
    public int LikeCount { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<SavedTutorial> SavedInLists { get; set; } = new List<SavedTutorial>();
}
