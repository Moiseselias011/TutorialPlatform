namespace TutorialPlatform.Api.Entities;

/// <summary>
/// Una visita registrada al cambiar de página en la SPA.
///
/// Nunca guarda la IP en claro: solo su hash. Basta para contar visitantes
/// distintos y no deja datos personales en la base (§5 no prohíbe las
/// estadísticas internas, pero no hay por qué conservar de dónde viene
/// cada visita).
/// </summary>
public class Visit
{
    public int Id { get; set; }

    /// <summary>
    /// Momento de la visita. Se guarda en HORA LOCAL: el servidor es el
    /// propio equipo del dueño, así que la hora que a él le interesa ver es
    /// la suya y no la de UTC (a las 01:00 de la madrugada UTC seguiría
    /// siendo «ayer»).
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Ruta normalizada: sin cadena de consulta y con los IDs numéricos
    /// colapsados a «{id}». Sin eso, /tutorials/39 y /tutorials/40 abrirían
    /// una fila cada una y el desglose por página no serviría para nada.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>SHA-256 en hexadecimal (64 caracteres) de la IP + sal fija.</summary>
    public string VisitorHash { get; set; } = string.Empty;
}
