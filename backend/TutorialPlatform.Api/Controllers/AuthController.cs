using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;
using TutorialPlatform.Api.Services;

namespace TutorialPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly ICurrentUser _current;

    public AuthController(AppDbContext db, ITokenService tokens, ICurrentUser current)
    {
        _db = db;
        _tokens = tokens;
        _current = current;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        // Validaciones de negocio adicionales a las del DataAnnotation
        var username = req.Username.Trim();
        var email = req.Email.Trim().ToLowerInvariant();

        if (username.Length < 3)
            return BadRequest(new { message = "El usuario debe tener al menos 3 caracteres." });

        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Conflict(new { message = "Ese nombre de usuario ya está en uso." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Ese correo ya está registrado." });

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Roles.User // el registro siempre crea USER; ADMIN se asigna a mano
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Token = _tokens.CreateToken(user),
            User = await ToDto(user)
        });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var id = req.UsernameOrEmail.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == id || u.Email == id.ToLowerInvariant());

        // Respuesta genérica para no revelar qué existe
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales incorrectas." });

        return Ok(new AuthResponse
        {
            Token = _tokens.CreateToken(user),
            User = await ToDto(user)
        });
    }

    // GET /api/auth/me  → solo el propio usuario (INVARIANTE)
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _db.Users.FindAsync(_current.Id);
        if (user is null) return Unauthorized();

        return Ok(await ToDto(user));
    }

    // PUT /api/auth/me  → editar solo el propio perfil
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(_current.Id);
        if (user is null) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            var email = req.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                return Conflict(new { message = "Ese correo ya está registrado." });
            user.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword) ||
                !BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        }

        // ---- Foto de perfil ----
        // Si el campo no viene en el body (null) no se toca. Cadena vacía = eliminar.
        if (req.PhotoUrl is not null)
        {
            var photo = req.PhotoUrl.Trim();

            if (photo.Length == 0)
            {
                user.PhotoUrl = null;
            }
            else
            {
                // Solo rutas propias bajo /uploads/: se descartan URLs externas,
                // rutas absolutas y cualquier intento de path traversal.
                var invalida =
                    !photo.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
                    photo.Contains("..") ||
                    photo.Contains('\\') ||
                    photo.Length > 500 ||
                    !Path.HasExtension(photo);

                if (invalida)
                    return BadRequest(new
                    {
                        message = "Ruta de imagen no válida: debe ser una ruta bajo /uploads/ devuelta por POST /api/images."
                    });

                user.PhotoUrl = photo;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(await ToDto(user));
    }

    private async Task<UserDto> ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt,
        PhotoUrl = u.PhotoUrl,
        // Suma de "Me gusta" recibidos por todos los tutoriales de este usuario
        TotalLikesReceived = await _db.Tutorials
            .Where(t => t.AuthorId == u.Id)
            .SumAsync(t => (int?)t.LikeCount) ?? 0
    };
}
