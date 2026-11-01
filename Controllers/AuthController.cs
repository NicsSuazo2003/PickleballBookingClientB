// Controllers/AuthController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ClientResolver _clientResolver;

    public AuthController(IAuthService auth, ClientResolver clientResolver)
    {
        _auth = auth;
        _clientResolver = clientResolver;
    }

    private string? Ip =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? Ua =>
        Request.Headers["User-Agent"].FirstOrDefault();

    // ✅ NEW — 409 (not 401) on missing tenant, via ResolveClientIdAsync
    private Task<Guid> GetClientId() => _clientResolver.ResolveClientIdAsync();

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var response = await _auth.LoginAsync(request, Ip, Ua);
        return Ok(response);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var clientId = await GetClientId();
        var response = await _auth.RegisterAsync(request, clientId);
        return Ok(response);
    }

    [HttpPost("register-staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AuthResponse>> RegisterStaff(RegisterRequest request)
    {
        var clientId = await GetClientId();
        var response = await _auth.RegisterStaffAsync(request, clientId);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var clientId = await GetClientId();
        await _auth.ForgotPasswordAsync(request.Email, clientId, Ip, Ua);

        // ✅ Always the same response — no email enumeration
        return Ok(new { message = "If an account exists with that email, a reset link has been sent." });
    }

    // ✅ NEW — Fix A: ResetPassword is a real endpoint now
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var clientId = await GetClientId();
        await _auth.ResetPasswordAsync(request, clientId, Ip, Ua);
        return Ok(new { message = "Password has been reset. You can now sign in." });
    }

    [Authorize, HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _auth.UpdateProfileAsync(userId, request);
        return Ok(user);
    }

    [Authorize, HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Password changed successfully" });
    }
}