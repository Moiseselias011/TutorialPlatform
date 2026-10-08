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

/// <summary>
/// Query del detalle de lista: búsqueda y paginación de los tutoriales guardados.
/// La lista no tiene tope de elementos, así que el recorte se hace en el SERVIDOR
/// (mismo criterio que <see cref="TutorialQuery"/>) en vez de traerlo todo entero.
/// </summary>
public class PersonalListQuery
{
    private const int MaxPageSize = 50;
    private int _page = 1;
    private int _pageSize = 6; // la vista de detalle muestra 6 por página

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 6 : Math.Min(value, MaxPageSize);
    }

    /// <summary>
    /// Texto libre del buscador. Cubre título, descripción, tecnología y autor:
    /// son los mismos campos que filtraba la versión hecha en cliente, así no se
    /// pierde capacidad (Home solo busca en título y descripción).
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>Lista con su página de tutoriales guardados (vista de detalle).</summary>
public class PersonalListDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Total REAL de tutoriales guardados, sin aplicar la búsqueda:
    /// es el contador de cabecera («N tutoriales guardados»).</summary>
    public int TutorialCount { get; set; }

    /// <summary>Página actual más los totales del filtro activo
    /// (<c>TotalCount</c> = cuántos coinciden con la búsqueda).</summary>
    public PagedResult<TutorialDto> Tutorials { get; set; } = new();
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
