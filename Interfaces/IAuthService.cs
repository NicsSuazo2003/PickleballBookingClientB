// Interfaces/IAuthService.cs
using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, Guid clientId);
    Task<AuthResponse> RegisterStaffAsync(RegisterRequest request, Guid clientId);

    /// <summary>
    /// Always returns success (never leaks whether the email exists).
    /// </summary>
    Task ForgotPasswordAsync(string email, Guid clientId, string? ip, string? userAgent);

    /// <summary>
    /// Consumes the reset token and sets a new password.
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, Guid clientId, string? ip, string? userAgent);

    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}