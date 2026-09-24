// Controllers/AuthController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ClientResolver _clientResolver;
    private readonly IClientService _clientService;

    public AuthController(IAuthService auth, ClientResolver clientResolver, IClientService clientService)
    {
        _auth = auth;
        _clientResolver = clientResolver;
        _clientService = clientService;
    }

    private async Task<Guid> GetClientId()
    {
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            throw new UnauthorizedAccessException("Client identification required");

        return await _clientService.GetClientIdBySubdomainAsync(subdomain);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var response = await _auth.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("register")]
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
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If an account exists, a reset link has been sent" });
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