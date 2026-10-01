using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Mvc;
using TutorialPlatform.Api.Dtos;

namespace TutorialPlatform.Api.Controllers;

/// <summary>
/// Subida y administración de imágenes (tutoriales / tecnologías / perfil).
/// Reglas: solo usuarios autenticados, tipos allow-list, tamaño máximo 5 MB.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };

    /// <summary>Máximo 5 MB.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public ImagesController(IWebHostEnvironment env) => _env = env;

    private string UploadRoot => Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");

    // POST /api/images  (multipart/form-data, campo "file")
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<ImageUploadResult>> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Debes seleccionar una imagen." });

        if (file.Length > MaxBytes)
            return BadRequest(new { message = "La imagen no puede superar 5 MB." });

        // ---- Validación de extensión ----
        var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"Tipo no permitido. Usa: {string.Join(", ", AllowedExtensions)}" });

        // ---- Validación de Content-Type (allow-list) ----
        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = $"Content-Type no permitido: {file.ContentType}" });

        // ---- Validación de firma binaria (no se puede engañar con extensión) ----
        if (!await LooksLikeImageAsync(file))
            return BadRequest(new { message = "El archivo no contiene datos de imagen válidos." });

        // ---- Nombre seguro: GUID, sin usar el nombre original del usuario ----
        var fileName = $"{Guid.NewGuid():N}{ext}";
        Directory.CreateDirectory(UploadRoot);
        var path = Path.Combine(UploadRoot, fileName);

        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new ImageUploadResult
        {
            Url = $"/uploads/{fileName}",
            FileName = fileName,
            Size = file.Length
        });
    }

    // DELETE /api/images/{fileName} → solo el ADMIN puede limpiar imágenes
    [HttpDelete("{fileName}")]
    [Authorize(Roles = Entities.Roles.Admin)]
    public IActionResult Delete(string fileName)
    {
        // Evita path traversal (../../etc/passwd)
        var safe = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safe))
            return BadRequest(new { message = "Nombre inválido." });

        var path = Path.Combine(UploadRoot, safe);
        if (!System.IO.File.Exists(path))
            return NotFound(new { message = "Imagen no encontrada." });

        System.IO.File.Delete(path);
        return NoContent();
    }

    // GET /api/images → listar imágenes subidas (ADMIN)
    [HttpGet]
    [Authorize(Roles = Entities.Roles.Admin)]
    public IActionResult List()
    {
        if (!Directory.Exists(UploadRoot)) return Ok(Array.Empty<object>());

        var files = Directory.GetFiles(UploadRoot)
            .Select(f => new
            {
                FileName = Path.GetFileName(f),
                Url = $"/uploads/{Path.GetFileName(f)}",
                Size = new FileInfo(f).Length,
                ModifiedAt = System.IO.File.GetLastWriteTimeUtc(f)
            })
            .OrderByDescending(f => f.ModifiedAt);

        return Ok(files);
    }

    /// <summary>Comprueba que el contenido real sea una imagen conocida.</summary>
    private static async Task<bool> LooksLikeImageAsync(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (bytes.Length < 12) return false;

        // PNG
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
        // JPEG
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;
        // GIF
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
        // WEBP ("RIFF" + "WEBP")
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;
        // SVG (texto que contiene "<svg")
        var head = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 512));
        if (head.Contains("<svg", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }
}
