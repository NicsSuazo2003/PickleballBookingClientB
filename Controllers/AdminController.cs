// Controllers/AdminController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IBookingService _booking;
    private readonly ICourtService _court;
    private readonly ClientResolver _clientResolver;
    private readonly IClientService _clientService;
    private readonly AppDbContext _db;

    public AdminController(
        IAdminService admin,
        IBookingService booking,
        ICourtService court,
        ClientResolver clientResolver,
        IClientService clientService,
        AppDbContext db)
    {
        _admin = admin;
        _booking = booking;
        _court = court;
        _clientResolver = clientResolver;
        _clientService = clientService;
        _db = db;
    }

    // ✅ Fix D — resolve tenant from JWT client_id claim, NOT from header
    private Guid GetClientId()
    {
        var claim = User.FindFirstValue("client_id");
        if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var cid) || cid == Guid.Empty)
            throw new InvalidOperationException("Client identification required");
        return cid;
    }

    // ═════════════════════════════════════════════════════════════
    // BOOKINGS
    // ═════════════════════════════════════════════════════════════
    [HttpGet("bookings")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<List<BookingDto>>> GetBookings()
    {
        var clientId = GetClientId();
        return Ok(await _booking.GetAllBookingsAsync(clientId));
    }

    [HttpPut("bookings/{id}")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<BookingDto>> UpdateBooking(Guid id, AdminUpdateBookingRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _booking.AdminUpdateBookingAsync(id, request, clientId));
    }

    [HttpPost("bookings/manual")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<BookingDto>> CreateManualBooking(StaffCreateBookingRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _booking.CreateStaffBookingAsync(request, clientId));
    }

    [HttpPatch("bookings/{id}/reschedule")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<RescheduleBookingResponse>> RescheduleBooking(Guid id, RescheduleBookingRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _booking.RescheduleBookingAsync(id, request, clientId));
    }

    // ═════════════════════════════════════════════════════════════
    // ANALYTICS
    // ═════════════════════════════════════════════════════════════
    [HttpGet("analytics")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics()
    {
        var clientId = GetClientId();
        return Ok(await _admin.GetAnalyticsAsync(clientId));
    }

    [HttpGet("analytics/courts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<CourtAnalyticsDto>>> GetCourtAnalytics()
    {
        var clientId = GetClientId();
        return Ok(await _admin.GetCourtAnalyticsAsync(clientId));
    }

    // ═════════════════════════════════════════════════════════════
    // COURTS
    // ═════════════════════════════════════════════════════════════
    [HttpGet("courts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<CourtDto>>> GetCourts()
    {
        var clientId = GetClientId();
        return Ok(await _court.GetAllCourtsAsync(clientId));
    }

    [HttpGet("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> GetCourt(Guid id)
    {
        var clientId = GetClientId();
        return Ok(await _court.GetCourtByIdAsync(id, clientId));
    }

    [HttpPost("courts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> CreateCourt(CreateCourtRequest request)
    {
        var clientId = GetClientId();
        var court = await _court.CreateCourtAsync(request, clientId);
        return CreatedAtAction(nameof(GetCourt), new { id = court.Id }, court);
    }

    [HttpPut("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> UpdateCourt(Guid id, UpdateCourtRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _court.UpdateCourtAsync(id, request, clientId));
    }

    [HttpDelete("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteCourt(Guid id)
    {
        var clientId = GetClientId();
        await _court.DeleteCourtAsync(id, clientId);
        return NoContent();
    }

    // ═════════════════════════════════════════════════════════════
    // SETTINGS
    // ═════════════════════════════════════════════════════════════
    [HttpGet("settings")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ClientDto>> GetSettings()
    {
        var clientId = GetClientId();
        var client = await _clientService.GetClientBySubdomainAsync(
            _clientResolver.GetSubdomain() ?? throw new InvalidOperationException("Client identification required"));
        return Ok(client);
    }

    [HttpPut("settings")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ClientDto>> UpdateSettings(UpdateClientSettingsRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _clientService.UpdateClientSettingsAsync(clientId, request));
    }

    // ═════════════════════════════════════════════════════════════
    // STAFF
    // ═════════════════════════════════════════════════════════════
    [HttpGet("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UserDto>>> GetStaff()
    {
        var clientId = GetClientId();
        return Ok(await _admin.GetStaffByClientAsync(clientId));
    }

    [HttpPost("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserDto>> CreateStaff(CreateStaffRequest request)
    {
        var clientId = GetClientId();
        return Ok(await _admin.CreateStaffAsync(request, clientId));
    }

    [HttpPut("staff/{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request)
    {
        var clientId = GetClientId();
        await _admin.UpdateStaffStatusAsync(id, request.Status, clientId);
        return Ok(new { message = "Staff status updated" });
    }

    [HttpDelete("staff/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        var clientId = GetClientId();
        await _admin.DeleteStaffAsync(id, clientId);
        return Ok(new { message = "Staff removed" });
    }

    // ═════════════════════════════════════════════════════════════
    // SECURITY EVENTS
    // ═════════════════════════════════════════════════════════════
    [HttpGet("security-events")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<SecurityEventDto>>> GetSecurityEvents(
        [FromQuery] string? eventType,
        [FromQuery] string? severity,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool hideRateLimit = true,
        [FromQuery] int take = 100)
    {
        var clientId = GetClientId();

        take = Math.Clamp(take, 1, 500);

        var q = _db.SecurityEvents
            .AsNoTracking()
            .Where(e => e.ClientId == clientId);

        if (!string.IsNullOrWhiteSpace(eventType))
            q = q.Where(e => e.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(severity))
            q = q.Where(e => e.Severity == severity);

        if (hideRateLimit)
            q = q.Where(e => e.EventType != "rate_limit.triggered");

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e =>
                (e.Message != null && EF.Functions.ILike(e.Message, $"%{search}%")) ||
                (e.IpHash != null && EF.Functions.ILike(e.IpHash, $"%{search}%")) ||
                (e.UserAgent != null && EF.Functions.ILike(e.UserAgent, $"%{search}%")));

        if (from is not null)
            q = q.Where(e => e.CreatedAt >= from.Value);

        if (to is not null)
            q = q.Where(e => e.CreatedAt <= to.Value);

        var rows = await q
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync();

        var dtos = rows.Select(e => new SecurityEventDto(
            e.Id.ToString(),
            e.ClientId.ToString(),
            e.IpHash,
            e.IpAddress,
            e.UserAgent,
            e.EventType,
            e.Severity,
            e.Message,
            string.IsNullOrEmpty(e.Metadata)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<object>(e.Metadata),
            e.Success,
            e.ErrorCode,
            e.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        )).ToList();

        return Ok(dtos);
    }

    // NOTE: /debug-headers and /debug-subdomain have been REMOVED.
    // They were [AllowAnonymous] information-disclosure vectors.
}