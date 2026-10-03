using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class Technology
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Dominio al que pertenece: <see cref="Domains.Programacion"/> (las
    /// tecnologías de siempre) o <see cref="Domains.Dibujo"/> (las técnicas).
    /// Cada índice de la aplicación filtra por el suyo, así que una tecnología
    /// no puede mezclar tutoriales de programación con los de dibujo.
    /// </summary>
    [Required, MaxLength(20)]
    public string Domain { get; set; } = Domains.Programacion;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
}

/// <summary>Dominios de contenido: separan los índices de la aplicación.</summary>
public static class Domains
{
    public const string Programacion = "programacion";
    public const string Dibujo = "dibujo";
    public const string Marketing = "marketing";

    /// <summary>Dominio que devuelve la API cuando en la query no llega ninguno.</summary>
    public const string PorDefecto = Programacion;

    /// <summary>
    /// Único sitio donde se lista lo permitido: así, añadir un índice nuevo
    /// no obliga a recordar en cuántos controladores hay el if.
    /// </summary>
    public static bool EsValido(string dominio) =>
        dominio == Programacion || dominio == Dibujo || dominio == Marketing;
}
