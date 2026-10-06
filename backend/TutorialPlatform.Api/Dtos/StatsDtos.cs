using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

// ---------- Requests ----------

public class RegistrarVisitRequest
{
    /// <summary>
    /// Ruta de la SPA ("/", "/dibujo", "/tutorials/39"…). El frontend envía
    /// <c>route.path</c>, que ya va sin cadena de consulta, y la trunca a 200
    /// antes de mandarla: la validación cliente y servidor coinciden.
    /// </summary>
    [Required(ErrorMessage = "La ruta es obligatoria.")]
    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "La ruta debe tener entre 1 y 200 caracteres.")]
    public string Path { get; set; } = string.Empty;
}

// ---------- Responses ----------

public class StatsResumenDto
{
    public int VisitasTotales { get; set; }

    /// <summary>Personas distintas (hash de IP) en todo el histórico.</summary>
    public int VisitantesUnicos { get; set; }

    public int VisitasHoy { get; set; }

    /// <summary>Cuántas rutas distintas se han visitado alguna vez.</summary>
    public int PaginasDistintas { get; set; }

    public List<PaginaVisitaDto> PorPagina { get; set; } = new();

    public List<DiaVisitaDto> PorDia { get; set; } = new();
}

public class PaginaVisitaDto
{
    public string Ruta { get; set; } = string.Empty;
    public int Visitas { get; set; }
    public int Unicos { get; set; }
}

public class DiaVisitaDto
{
    /// <summary>Solo interesa la fecha (00:00:00); el frontend la pinta como «dd/mm».</summary>
    public DateTime Fecha { get; set; }
    public int Visitas { get; set; }
    public int Unicos { get; set; }
}
