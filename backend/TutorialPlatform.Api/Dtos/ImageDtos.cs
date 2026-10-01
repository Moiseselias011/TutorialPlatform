using System.ComponentModel.DataAnnotations;

namespace TutorialPlatform.Api.Dtos;

public class ImageUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class UploadImageRequest
{
    /// <summary>Archivo enviado por multipart/form-data.</summary>
    [Required]
    public IFormFile? File { get; set; }
}
