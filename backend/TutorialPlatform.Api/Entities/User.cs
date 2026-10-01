using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Entities;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>USER o ADMIN</summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = Roles.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foto de perfil: ruta relativa bajo /uploads/ (p. ej. "/uploads/abc.jpg") o null.
    /// Solo se admiten rutas de ese prefijo: nunca una URL externa.
    /// </summary>
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    // Relaciones
    public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<PersonalList> PersonalLists { get; set; } = new List<PersonalList>();
}

public static class Roles
{
    public const string User = "USER";
    public const string Admin = "ADMIN";
}
