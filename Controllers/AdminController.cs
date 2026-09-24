// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
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

    public AdminController(
        IAdminService admin,
        IBookingService booking,
        ICourtService court,
        ClientResolver clientResolver,
        IClientService clientService)
    {
        _admin = admin;
        _booking = booking;
        _court = court;
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

    // ========================================
    // ✅ STAFF & ADMIN ACCESS
    // ========================================

    [HttpGet("bookings")]
    [Authorize(Roles = "admin,staff")]  // ✅ Allow both roles
    public async Task<ActionResult<List<BookingDto>>> GetBookings()
    {
        var clientId = await GetClientId();
        var bookings = await _booking.GetAllBookingsAsync(clientId);
        return Ok(bookings);
    }

    [HttpPut("bookings/{id}")]
    [Authorize(Roles = "admin,staff")]  // ✅ Allow both roles
    public async Task<ActionResult<BookingDto>> UpdateBooking(Guid id, AdminUpdateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.AdminUpdateBookingAsync(id, request, clientId);
        return Ok(booking);
    }
    [HttpPost("bookings/manual")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<BookingDto>> CreateManualBooking(StaffCreateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.CreateStaffBookingAsync(request, clientId);
        return Ok(booking);
    }

    // ========================================
    // ✅ ADMIN ONLY ACCESS
    // ========================================

    [HttpGet("analytics")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics()
    {
        var clientId = await GetClientId();
        var analytics = await _admin.GetAnalyticsAsync(clientId);
        return Ok(analytics);
    }

    [HttpGet("analytics/courts")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<List<CourtAnalyticsDto>>> GetCourtAnalytics()
    {
        var clientId = await GetClientId();
        var analytics = await _admin.GetCourtAnalyticsAsync(clientId);
        return Ok(analytics);
    }

    [HttpGet("courts")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<List<CourtDto>>> GetCourts()
    {
        var clientId = await GetClientId();
        var courts = await _court.GetAllCourtsAsync(clientId);
        return Ok(courts);
    }

    [HttpGet("courts/{id}")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<CourtDto>> GetCourt(Guid id)
    {
        var clientId = await GetClientId();
        var court = await _court.GetCourtByIdAsync(id, clientId);
        return Ok(court);
    }

    [HttpPost("courts")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<CourtDto>> CreateCourt(CreateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.CreateCourtAsync(request, clientId);
        return CreatedAtAction(nameof(GetCourt), new { id = court.Id }, court);
    }

    [HttpPut("courts/{id}")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<CourtDto>> UpdateCourt(Guid id, UpdateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.UpdateCourtAsync(id, request, clientId);
        return Ok(court);
    }

    [HttpDelete("courts/{id}")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<IActionResult> DeleteCourt(Guid id)
    {
        var clientId = await GetClientId();
        await _court.DeleteCourtAsync(id, clientId);
        return NoContent();
    }

    [HttpGet("settings")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<ClientDto>> GetSettings()
    {
        var subdomain = _clientResolver.GetSubdomain();
        var client = await _clientService.GetClientBySubdomainAsync(subdomain!);
        return Ok(client);
    }

    [HttpPut("settings")]
    [Authorize(Roles = "admin")]  // ✅ Admin only
    public async Task<ActionResult<ClientDto>> UpdateSettings(UpdateClientSettingsRequest request)
    {
        var clientId = await GetClientId();
        var client = await _clientService.UpdateClientSettingsAsync(clientId, request);
        return Ok(client);
    }

    // ========================================
    // ⚠️ DEBUG ENDPOINTS
    // ========================================

    [HttpGet("debug-headers")]
    [AllowAnonymous]
    public IActionResult DebugHeaders()
    {
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        return Ok(headers);
    }

    [HttpGet("debug-subdomain")]
    [AllowAnonymous]
    public IActionResult DebugSubdomain()
    {
        var subdomain = _clientResolver.GetSubdomain();
        return Ok(new { subdomain, host = Request.Host.Host });
    }
    // Controllers/AdminController.cs - Add these endpoints

    [HttpGet("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UserDto>>> GetStaff()
    {
        var clientId = await GetClientId();
        var users = await _admin.GetStaffByClientAsync(clientId);
        return Ok(users);
    }

    [HttpPost("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserDto>> CreateStaff(CreateStaffRequest request)
    {
        var clientId = await GetClientId();
        var user = await _admin.CreateStaffAsync(request, clientId);
        return Ok(user);
    }

    [HttpPut("staff/{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request)
    {
        var clientId = await GetClientId();
        await _admin.UpdateStaffStatusAsync(id, request.Status, clientId);
        return Ok(new { message = "Staff status updated" });
    }

    [HttpDelete("staff/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> DeleteStaff(Guid id)
    {
        var clientId = await GetClientId();
        await _admin.DeleteStaffAsync(id, clientId);
        return Ok(new { message = "Staff removed" });
    }
}