using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class Technology
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
}
