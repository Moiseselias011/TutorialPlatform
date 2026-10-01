using System.Security.Claims;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Services;

/// <summary>Helper para leer el usuario autenticado desde los claims del JWT.</summary>
public interface ICurrentUser
{
    int Id { get; }
    string Role { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
    User AsUser(); // usuario mínimo para firmar tokens
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int Id
    {
        get
        {
            var v = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }
    }

    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAdmin => Role == Roles.Admin;

    public User AsUser() => new() { Id = Id };
}
