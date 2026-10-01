using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

// ---------- Requests ----------

public class RegisterRequest
{
    // Mínimo 3 para ALINEARLO con la validación del formulario de registro
    [Required, MinLength(3, ErrorMessage = "El usuario debe tener al menos 3 caracteres."), MaxLength(50)]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "El usuario solo puede contener letras, números, punto, guion y guion bajo.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [MinLength(6), MaxLength(100)]
    public string? NewPassword { get; set; }

    [MaxLength(100)]
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Nueva foto de perfil: ruta bajo "/uploads/" (lo devuelve POST /api/images).
    /// Cadena vacía ("") elimina la foto actual. Si el campo no viene en el body, no se toca.
    /// </summary>
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }
}

// ---------- Responses ----------

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Foto de perfil (ruta "/uploads/...") o null si no tiene.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Distribución de "Me gusta" recibidos por el usuario.</summary>
    public int TotalLikesReceived { get; set; }
}
