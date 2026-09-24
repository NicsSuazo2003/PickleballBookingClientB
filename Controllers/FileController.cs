using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/files")]
[Authorize(Roles = "admin")]
public class FileController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public FileController(IConfiguration config, HttpClient http)
    {
        _config = config;
        _http = http;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF are allowed" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File must be under 5MB" });

        var supabaseUrl = _config["Supabase:Url"]!;
        var supabaseKey = _config["Supabase:Key"]!;
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

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
            return BadRequest(new { message = "Upload failed" });

        var url = $"{supabaseUrl}/storage/v1/object/public/PickleImgs/{fileName}";
        return Ok(new { url });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteFileRequest request)
    {
        if (string.IsNullOrEmpty(request.Url)) return BadRequest();

        var supabaseUrl = _config["Supabase:Url"]!;
        var supabaseKey = _config["Supabase:Key"]!;
        var fileName = Path.GetFileName(new Uri(request.Url).AbsolutePath);

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"{supabaseUrl}/storage/v1/object/PickleImgs/{fileName}");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);

        await _http.SendAsync(httpRequest);
        return Ok(new { message = "File deleted" });
    }
}

public record DeleteFileRequest(string Url);