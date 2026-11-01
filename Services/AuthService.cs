// Services/AuthService.cs
using System.Security.Cryptography;
using System.Text;
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
    private readonly EmailService _email;
    private readonly ISecurityLogger _security;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResetTokenTtl = TimeSpan.FromMinutes(60);

    public AuthService(
        AppDbContext db,
        TokenService tokenService,
        EmailService email,
        ISecurityLogger security,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _email = email;
        _security = security;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Constant-time dummy compare for unknown emails — no timing leak
        if (user is null)
        {
            _ = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11);

            // Fix C — resolve the tenant so the event isn't Guid.Empty
            var tenantId = await _db.Clients
                .Where(c => c.Status == "active")
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();

            if (tenantId is not null)
            {
                await _security.LogAuthFailureAsync(
                    tenantId.Value,
                    request.Email,
                    ip,
                    userAgent,
                    "auth.login_failed_unknown_email",
                    "Login attempt for unknown email");
            }

            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Fix B — reject if currently locked out
        if (user.LockoutUntil is not null && user.LockoutUntil > DateTime.UtcNow)
        {
            var minutes = (int)Math.Ceiling((user.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes);

            await _security.LogAuthFailureAsync(
                user.ClientId,
                request.Email,
                ip,
                userAgent,
                "auth.login_locked",
                $"Account locked for {minutes} more minute(s)");

            throw new UnauthorizedAccessException(
                $"Account is temporarily locked. Try again in {minutes} minute(s).");
        }

        // Fix B — verify password; on failure increment counter + possibly lock
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;

                await _db.SaveChangesAsync();

                await _security.LogAuthFailureAsync(
                    user.ClientId,
                    request.Email,
                    ip,
                    userAgent,
                    "auth.login_locked",
                    $"Account locked after {MaxFailedLogins} failed attempts");

                throw new UnauthorizedAccessException(
                    $"Too many failed attempts. Account locked for {LockoutDuration.TotalMinutes:0} minutes.");
            }

            await _db.SaveChangesAsync();

            await _security.LogAuthFailureAsync(
                user.ClientId,
                request.Email,
                ip,
                userAgent,
                "auth.login_failed",
                $"Failed attempt {user.FailedLoginCount}/{MaxFailedLogins}");

            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status == "suspended")
        {
            await _security.LogAuthFailureAsync(
                user.ClientId,
                request.Email,
                ip,
                userAgent,
                "auth.login_suspended",
                "Login attempt by suspended user");

            throw new UnauthorizedAccessException("Account is suspended");
        }

        // Fix B — successful login resets the counter + lockout
        if (user.FailedLoginCount != 0 || user.LockoutUntil is not null)
        {
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            await _db.SaveChangesAsync();
        }

        return new AuthResponse(_tokenService.GenerateToken(user), MapToDto(user));
    }

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

    public async Task ForgotPasswordAsync(string email, Guid clientId, string? ip, string? userAgent)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is not null)
        {
            var token = GenerateResetToken();
            var hash = HashResetToken(token);

            user.PasswordResetTokenHash = hash;
            user.PasswordResetExpiresAt = DateTime.UtcNow.Add(ResetTokenTtl);
            await _db.SaveChangesAsync();

            try
            {
                await _email.SendPasswordResetEmailAsync(user.Email, user.Name, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Swallow — do not leak that the email send failed
            }

            await _security.LogAsync(new SecurityEvent
            {
                ClientId = clientId,
                IpHash = _security.HashIp(ip),
                IpAddress = ip,
                UserAgent = userAgent,
                EventType = "auth.password_reset_requested",
                Severity = "info",
                Message = "Password reset requested",
                Success = true,
            });
        }
        // user null → do nothing. Caller always returns 200. No leak.
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, Guid clientId, string? ip, string? userAgent)
    {
        var hash = HashResetToken(request.Token);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hash);
        if (user is null)
            throw new InvalidOperationException("Reset link is invalid or has expired.");

        if (user.PasswordResetExpiresAt is null || user.PasswordResetExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Reset link is invalid or has expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;

        await _db.SaveChangesAsync();

        await _security.LogAsync(new SecurityEvent
        {
            ClientId = user.ClientId,
            IpHash = _security.HashIp(ip),
            IpAddress = ip,
            UserAgent = userAgent,
            EventType = "auth.password_reset_completed",
            Severity = "info",
            Message = "Password reset completed",
            Success = true,
        });
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

        await _security.LogAsync(new SecurityEvent
        {
            ClientId = user.ClientId,
            EventType = "auth.password_changed",
            Severity = "info",
            Message = "Password changed via change-password endpoint",
            Success = true,
        });
    }

    private static string GenerateResetToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();   // 64 chars
    }

    private static string HashResetToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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