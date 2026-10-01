namespace TutorialPlatform.Api.Entities;

public class Like
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int TutorialId { get; set; }
    public Tutorial Tutorial { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
