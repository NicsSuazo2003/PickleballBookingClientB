// Controllers/FileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/files")]
[Authorize(Roles = "admin")]
public class FileController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly ILogger<FileController> _logger;

    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public FileController(IConfiguration config, HttpClient http, ILogger<FileController> logger)
    {
        _config = config;
        _http = http;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // ✅ Size limit from config, falls back to 5 MB
        var maxBytes = _config.GetValue<long?>("FileUpload:MaxSizeBytes") ?? (5 * 1024 * 1024);
        if (file.Length > maxBytes)
        {
            var mb = maxBytes / (1024.0 * 1024.0);
            return BadRequest(new { message = $"File must be under {mb:0.#} MB" });
        }

        // ✅ Allowed MIME list from config extensions, but still enforced by MIME
        var allowed = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (!MimeToExtension.TryGetValue(file.ContentType, out var ext)
            || !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF are allowed" });
        }

        var supabaseUrl = _config["Supabase:Url"]!;
        var supabaseKey = _config["Supabase:Key"]!;
        var fileName = $"{Guid.NewGuid()}{ext}";

        using var content = new StreamContent(file.OpenReadStream());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{supabaseUrl}/storage/v1/object/PickleImgs/{fileName}")
        {
            Content = content
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Supabase upload failed: {Status} {Body}",
                response.StatusCode,
                await response.Content.ReadAsStringAsync());
            return BadRequest(new { message = "Upload failed" });
        }

        var url = $"{supabaseUrl}/storage/v1/object/public/PickleImgs/{fileName}";
        return Ok(new { url });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteFileRequest request)
    {
        if (string.IsNullOrEmpty(request.Url)) return BadRequest();

        var supabaseUrl = _config["Supabase:Url"]!;
        var supabaseKey = _config["Supabase:Key"]!;

        string fileName;
        try
        {
            fileName = Path.GetFileName(new Uri(request.Url).AbsolutePath);
        }
        catch
        {
            return BadRequest(new { message = "Invalid URL" });
        }

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"{supabaseUrl}/storage/v1/object/PickleImgs/{fileName}");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);

        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase delete failed: {Status}", response.StatusCode);
        }

        return Ok(new { message = "File deleted" });
    }
}

public record DeleteFileRequest(string Url);