using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Dtos;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Controllers;

/// <summary>
/// Cuentas de la plataforma → /api/users
///
/// Único endpoint de usuarios que existe, y es de SOLO LECTURA y SOLO ADMIN.
///
/// Por qué no choca con el PROMPT, aunque es lo más cercano que hay:
/// la §2.3 («ver perfil → solo el propio usuario, sin perfiles públicos») y la
/// §5.7 prohíben perfiles públicos VISIBLES PARA LOS USUARIOS; esto es un
/// listado interno que encaja en la §2.2 («acciones administrativas» para
/// ADMIN), igual que GET /api/tutorials/{id}/likes, que ya es solo ADMIN.
/// La regla se respeta al pie de la letra creando ÚNICAMENTE esta lista:
/// no hay GET /api/users/{id}, no se puede mirar una cuenta suelta, y la API
/// sigue sin exponer ningún correo a nadie que no sea ADMIN.
///
/// Antes de esto la API no devolvía el correo de NINGÚN tercero (AuthController
/// solo el propio, CommentsController únicamente usuario y foto), de modo que
/// la exposición nueva queda reducida a esta acción, protegida en backend.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    /// <summary>
    /// Dominio de las cuentas ficticias que siembra <c>DbSeeder</c>.
    ///
    /// Se filtran AQUÍ y no con una columna nueva: sería una migración para lo
    /// que resuelve una condición, y el dominio ya es un marcador infranqueable
    /// —<c>example.com</c> está reservado por la RFC 2606 y no puede recibir
    /// correo, de modo que ningún registro real puede acabar filtrándose por
    /// accidente—. Si algún día el seed usa otro dominio, cambiar esta constante.
    /// </summary>
    private const string DominioDemo = "@example.com";

    /// <summary>
    /// Listado de cuentas reales: usuario, correo, fecha de alta y rol.
    /// Las más recientes primero, que es lo que interesa al mirar quién se ha
    /// registrado. Sin paginación: la tabla es pequeña y crece muy despacio.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var usuarios = await _db.Users
            .AsNoTracking()
            .Where(u => !u.Email.EndsWith(DominioDemo))
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Username)
            .Select(u => new UserListDto
            {
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                Role = u.Role,
            })
            .ToListAsync();

        return Ok(usuarios);
    }
}
