using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;
using PickleballBookingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Rebuild configuration FIRST
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// THEN bind the URL, so it isn't wiped out
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<EmailService>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IOpenPlayService, OpenPlayService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ClientResolver>();
builder.Services.AddScoped<IClientService, ClientService>();

// Health Checks
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen();

// CORS — driven entirely by appsettings.json + env vars.
// For PaddlePlace, set Cors__AllowedOrigins__0, __1, ... on Render.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:5173",
        "http://localhost:3000"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// Seed the database — safe to run every startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Only seeds if the DB is completely empty.
    // For a fresh PaddlePlace DB, this does nothing — you'll insert the client row manually.
    DbSeeder.Initialize(db);

    // Resolve the client for housekeeping tasks.
    // Tries env var CONFIG first, falls back to first client in the DB.
    var subdomain = builder.Configuration["Client:Subdomain"];
    Guid clientId = Guid.Empty;

    if (!string.IsNullOrWhiteSpace(subdomain))
    {
        try
        {
            var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
            clientId = await clientService.GetClientIdBySubdomainAsync(subdomain);
        }
        catch
        {
            // ignore — will fall back below
        }
    }

    if (clientId == Guid.Empty)
    {
        var firstClient = await db.Clients.FirstOrDefaultAsync();
        clientId = firstClient?.Id ?? Guid.Empty;
    }

    if (clientId != Guid.Empty)
    {
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        await bookingService.AutoCompletePastBookingsAsync(clientId);
        await bookingService.CancelExpiredPaymentsAsync(clientId);
    }
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");

app.UseSwagger();
app.UseSwaggerUI();
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