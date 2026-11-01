// DTOs/AuthDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace PickleballBookingSystem.DTOs;

public record LoginRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(1), MaxLength(128)] string Password);

public record RegisterRequest(
    [Required, MaxLength(100)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [MaxLength(32)] string Phone,
    [Required, MinLength(8), MaxLength(128)] string Password);

public record ForgotPasswordRequest(
    [Required, EmailAddress, MaxLength(254)] string Email);

public record ResetPasswordRequest(
    [Required, MinLength(16), MaxLength(128)] string Token,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);

public record UpdateProfileRequest(
    [MaxLength(100)] string? Name,
    [EmailAddress, MaxLength(254)] string? Email,
    [MaxLength(32)] string? Phone);

public record AuthResponse(string Token, UserDto User);

public record ChangePasswordRequest(
    [Required, MinLength(1), MaxLength(128)] string CurrentPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);