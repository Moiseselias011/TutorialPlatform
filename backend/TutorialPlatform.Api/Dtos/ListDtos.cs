using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

// ---------- Listas personales ----------

public class CreateListRequest
{
    [Required, MaxLength(120)]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateListRequest : CreateListRequest { }

public class PersonalListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TutorialCount { get; set; }
}

/// <summary>Lista con sus tutoriales guardados (vista de detalle).</summary>
public class PersonalListDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<TutorialDto> Tutorials { get; set; } = Enumerable.Empty<TutorialDto>();
}

// ---------- Guardar / quitar tutorial ----------

/// <summary>Body del modal "Guardar tutorial": el tutorial a guardar.
/// La lista destino viene en la ruta (/api/lists/{id}/save).</summary>
public class SaveTutorialRequest
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Debes indicar un tutorial válido.")]
    public int TutorialId { get; set; }
}

public class SaveResult
{
    public bool Saved { get; set; }
    public int PersonalListId { get; set; }
    public string ListName { get; set; } = string.Empty;
    public int TutorialCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
