using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;
using PickleballBookingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ─── Database ─────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT ──────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key must be configured (use env var, never commit it).");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ─── ForwardedHeaders (protocol only — IP handled below) ──
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

static bool IsNonPublic(IPAddress ip)
{
    if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
    if (IPAddress.IsLoopback(ip)) return true;

    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || (ip.GetAddressBytes()[0] & 0xFE) == 0xFC;

    var b = ip.GetAddressBytes();
    return b[0] == 0
        || b[0] == 10
        || (b[0] == 172 && b[1] is >= 16 and <= 31)
        || (b[0] == 192 && b[1] == 168)
        || (b[0] == 169 && b[1] == 254)
        || (b[0] == 100 && b[1] is >= 64 and <= 127);
}

// Real visitor IP: CF-Connecting-IP > True-Client-IP > rightmost-public of XFF.
static IPAddress? ResolveClientIp(HttpContext ctx)
{
    foreach (var name in new[] { "CF-Connecting-IP", "True-Client-IP" })
    {
        var v = ctx.Request.Headers[name].FirstOrDefault()?.Trim();
        if (v is not null && IPAddress.TryParse(v, out var ip) && !IsNonPublic(ip))
            return ip;
    }

    var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(xff))
    {
        var parts = xff.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (IPAddress.TryParse(parts[i], out var ip) && !IsNonPublic(ip))
                return ip;
        }
    }

    return null;
}

// ─── Rate-limit rejection log gate ───
var rejectionLogGate = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();

static bool ShouldLogRejection(
    System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> gate,
    string key,
    TimeSpan window)
{
    if (gate.Count > 10_000) gate.Clear();

    var now = DateTime.UtcNow;
    while (true)
    {
        if (!gate.TryGetValue(key, out var last))
        {
            if (gate.TryAdd(key, now)) return true;
            continue;
        }
        if (now - last < window) return false;
        if (gate.TryUpdate(key, now, last)) return true;
    }
}

builder.Services.AddHttpClient();

// ─── Rate Limiter ─────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    static string ClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var route = ctx.Request.Path.Value ?? "/";
        var prefix =
            route.StartsWith("/api/bookings", StringComparison.Ordinal) ? "bookings" :
            route.StartsWith("/api/open-play", StringComparison.Ordinal) ? "openplay" :
            route.StartsWith("/api/auth", StringComparison.Ordinal) ? "auth" :
            route.StartsWith("/api/courts", StringComparison.Ordinal) ? "courts" :
            route.StartsWith("/api/clients", StringComparison.Ordinal) ? "clients" :
            "other";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"{prefix}:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = prefix switch
                {
                    "bookings" => 60,
                    "openplay" => 60,
                    "auth" => 20,
                    "courts" => 120,
                    "clients" => 120,
                    _ => 120,
                },
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
            });
    });

    options.AddPolicy("booking", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"booking-create:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 10,
                QueueLimit = 0,
            }));

    options.AddPolicy("auth", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"auth:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(15),
                SegmentsPerWindow = 15,
                QueueLimit = 0,
            }));

    options.AddPolicy("track", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"track:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 10,
                QueueLimit = 0,
            }));

    options.AddPolicy("upload", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"upload:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 12,
                QueueLimit = 0,
            }));

    options.AddPolicy("join", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"join:{ClientIp(ctx)}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 12,
                QueueLimit = 0,
            }));

    options.OnRejected = async (ctx, token) =>
    {
        var http = ctx.HttpContext;

        try
        {
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Sampled: at most one DB row per IP per 5 minutes
            if (ShouldLogRejection(rejectionLogGate, ip, TimeSpan.FromMinutes(5)))
            {
                var secLogger = http.RequestServices.GetService<ISecurityLogger>();
                if (secLogger is not null)
                {
                    var cid = http.Items["ResolvedClientId"] is Guid g ? g : Guid.Empty;
                    var ua = http.Request.Headers["User-Agent"].FirstOrDefault();

                    await secLogger.LogAsync(new PickleballBookingSystem.Entities.SecurityEvent
                    {
                        ClientId = cid,
                        IpHash = secLogger.HashIp(ip),
                        IpAddress = ip == "unknown" ? null : ip,
                        UserAgent = ua,
                        EventType = "rate_limit.triggered",
                        Severity = "warning",
                        Message = $"Rate limit hit on {http.Request.Method} {http.Request.Path} (sampled: 1 per 5 min per IP)",
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            path = http.Request.Path.Value,
                            method = http.Request.Method,
                        }),
                        Success = false,
                        ErrorCode = "429",
                    });
                }
            }
        }
        catch { }

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        http.Response.Headers.RetryAfter = "60";
        http.Response.ContentType = "application/json";
        await http.Response.WriteAsync(
            """{"message":"Too many requests. Please slow down and try again shortly."}""",
            token);
    };
});

// ─── Services ────────────────────────────────────────────
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ClientResolver>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IOpenPlayService, OpenPlayService>();
builder.Services.AddScoped<IClientService, ClientService>();

builder.Services.AddScoped<ISecurityLogger, SecurityLogger>();
builder.Services.AddHostedService<ExpiredBookingWorker>();
builder.Services.AddHostedService<SecurityRetentionWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen();

// ─── CORS ────────────────────────────────────────────────
// Union configured origins with required-origin allowlist so config
// drift cannot break production CORS.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

var requiredOrigins = new[]
{
    "https://paddleplace.vercel.app",
    "http://localhost:5173",
    "http://localhost:3000",
};

var finalCorsOrigins = corsOrigins
    .Union(requiredOrigins, StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(finalCorsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ─── Startup task ─────────────────────────────────────────
_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try { DbSeeder.Initialize(db); }
        catch (Exception ex) { logger.LogError(ex, "Seeding failed; app will start with empty DB."); }

        var clientIds = await db.Clients.Select(c => c.Id).ToListAsync();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        foreach (var cid in clientIds)
        {
            try
            {
                await bookingService.AutoCompletePastBookingsAsync(cid);
                await bookingService.CancelExpiredPaymentsAsync(cid);
            }
            catch { }
        }
    }
    catch { }
});

// ─── Pipeline ────────────────────────────────────────────
app.UseForwardedHeaders();

// Rewrites RemoteIpAddress to the real visitor IP BEFORE ClientResolver,
// RateLimiter, controllers and SecurityLogger read it.
app.Use(async (ctx, next) =>
{
    var real = ResolveClientIp(ctx);
    if (real is not null) ctx.Connection.RemoteIpAddress = real;
    await next();
});

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("Frontend");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"status\":\"healthy\"}");
    }
});

app.Run();