namespace TutorialPlatform.Api.Entities;

/// <summary>Tabla pivote N-M: tutorial guardado en una lista personal.</summary>
public class SavedTutorial
{
    public int Id { get; set; }

    public int PersonalListId { get; set; }
    public PersonalList PersonalList { get; set; } = null!;

    public int TutorialId { get; set; }
    public Tutorial Tutorial { get; set; } = null!;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
