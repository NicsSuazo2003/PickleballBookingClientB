// Interfaces/IAuthService.cs
using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, Guid clientId); // ✅ Add clientId parameter
    Task ForgotPasswordAsync(string email);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<AuthResponse> RegisterStaffAsync(RegisterRequest request, Guid clientId); // ✅ Add this
}