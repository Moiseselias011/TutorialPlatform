using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class PersonalList
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SavedTutorial> SavedTutorials { get; set; } = new List<SavedTutorial>();
}
