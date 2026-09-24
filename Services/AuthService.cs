// Services/AuthService.cs
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;
using BCrypt.Net;

namespace PickleballBookingSystem.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthService(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (user.Status == "suspended")
            throw new UnauthorizedAccessException("Account is suspended");

        return new AuthResponse(_tokenService.GenerateToken(user), MapToDto(user));
    }

    // ✅ RegisterAsync with clientId
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, Guid clientId)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("An account with this email already exists");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "user",
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Status = "active"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(_tokenService.GenerateToken(user), MapToDto(user));
    }

    // ✅ RegisterStaffAsync with clientId
    public async Task<AuthResponse> RegisterStaffAsync(RegisterRequest request, Guid clientId)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("An account with this email already exists");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "staff",
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Status = "active"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(_tokenService.GenerateToken(user), MapToDto(user));
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) throw new KeyNotFoundException("No account found with that email address");
        // TODO: Send reset email in production
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (request.Name is not null) user.Name = request.Name;
        if (request.Email is not null) user.Email = request.Email;
        if (request.Phone is not null) user.Phone = request.Phone;

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new InvalidOperationException("New password must be at least 8 characters");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
    }

    private static UserDto MapToDto(User u) => new(
        u.Id.ToString(),
        u.Name,
        u.Email,
        u.Phone,
        u.Role,
        u.Avatar,
        u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        u.BookingsCount,
        u.Status
    );
}