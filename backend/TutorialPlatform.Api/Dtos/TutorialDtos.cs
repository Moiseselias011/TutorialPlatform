using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

// ---------- Requests ----------

public class CreateTutorialRequest
{
    // Mínimos ALINEADOS con las validaciones del frontend (cliente y servidor idénticas)
    [Required, MinLength(3, ErrorMessage = "El título debe tener al menos 3 caracteres."), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10, ErrorMessage = "La descripción debe tener al menos 10 caracteres."), MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    [Url(ErrorMessage = "La URL debe ser válida (https://...)")]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar una tecnología.")]
    public int TechnologyId { get; set; }
}

public class UpdateTutorialRequest : CreateTutorialRequest { }

// ---------- Queries ----------

public class TutorialQuery
{
    private const int MaxPageSize = 50;
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : Math.Min(value, MaxPageSize);
    }

    /// <summary>Texto libre del buscador (título o descripción).</summary>
    public string? Search { get; set; }

    /// <summary>Filtro por tecnología: id numérico o slug.</summary>
    public string? Technology { get; set; }

    /// <summary>published | likes | title</summary>
    public string? Sort { get; set; }
}

// ---------- Responses ----------

public class TutorialDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TechnologyId { get; set; }
    public string Technology { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public int LikeCount { get; set; }

    /// <summary>Si el usuario autenticado ya dio Me gusta (null si no hay sesión).</summary>
    public bool? LikedByMe { get; set; }

    /// <summary>Si el usuario autenticado ya lo guardó en alguna de sus listas.</summary>
    public bool SavedByMe { get; set; }

    /// <summary>Si el usuario autenticado puede editarlo (es autor o ADMIN).</summary>
    public bool CanEdit { get; set; }

    /// <summary>Solo ADMIN. El frontend oculta el botón, pero la regla vive en el backend.</summary>
    public bool CanDelete { get; set; }

    public int CommentCount { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
