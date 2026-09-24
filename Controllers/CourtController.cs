using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController]
[Route("api/courts")]
public class CourtController : ControllerBase
{
    private readonly ICourtService _court;
    private readonly ClientResolver _clientResolver;
    private readonly IClientService _clientService;

    public CourtController(
        ICourtService court,
        ClientResolver clientResolver,
        IClientService clientService)
    {
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
    // ✅ PUBLIC ENDPOINTS
    // ========================================

    [HttpGet]
    public async Task<ActionResult<List<CourtDto>>> GetAllCourts()
    {
        var clientId = await GetClientId();
        var courts = await _court.GetAllCourtsAsync(clientId);
        return Ok(courts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourtDto>> GetCourt(Guid id)
    {
        var clientId = await GetClientId();
        var court = await _court.GetCourtByIdAsync(id, clientId);
        return Ok(court);
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<List<TimeSlotAvailabilityDto>>> GetAvailability(Guid id, [FromQuery] DateTime date)
    {
        var clientId = await GetClientId();
        var slots = await _court.GetCourtAvailabilityAsync(id, date, clientId);
        return Ok(slots);
    }

    [HttpGet("{id}/blocked-dates")]
    public async Task<ActionResult<List<BlockedDateDto>>> GetBlockedDates(Guid id)
    {
        var clientId = await GetClientId();
        var blockedDates = await _court.GetBlockedDatesAsync(id, clientId);
        return Ok(blockedDates);
    }

    // ========================================
    // ✅ ADMIN ENDPOINTS
    // ========================================

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<CourtDto>> CreateCourt(CreateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.CreateCourtAsync(request, clientId);
        return CreatedAtAction(nameof(GetCourt), new { id = court.Id }, court);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CourtDto>> UpdateCourt(Guid id, UpdateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.UpdateCourtAsync(id, request, clientId);
        return Ok(court);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourt(Guid id)
    {
        var clientId = await GetClientId();
        await _court.DeleteCourtAsync(id, clientId);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id}/blocked-dates")]
    public async Task<ActionResult<BlockedDateDto>> AddBlockedDate(Guid id, CreateBlockedDateRequest request)
    {
        var clientId = await GetClientId();
        var result = await _court.AddBlockedDateAsync(request, id, clientId);
        return CreatedAtAction(nameof(GetBlockedDates), new { id }, result);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("blocked-dates/{blockedId}")]
    public async Task<IActionResult> DeleteBlockedDate(Guid blockedId)
    {
        var clientId = await GetClientId();
        await _court.DeleteBlockedDateAsync(blockedId, clientId);
        return NoContent();
    }

    // ========================================
    // ⚠️ DEPRECATED - Single Court Endpoints (Backward Compatibility)
    // ========================================

    [HttpGet("legacy")]
    public async Task<ActionResult<CourtDto>> GetCourtLegacy()
    {
        var court = await _court.GetCourtAsync();
        return Ok(court);
    }

    [HttpGet("legacy/availability")]
    public async Task<ActionResult<List<TimeSlotAvailabilityDto>>> GetAvailabilityLegacy([FromQuery] DateTime date)
    {
        var slots = await _court.GetAvailabilityAsync(date);
        return Ok(slots);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("legacy/settings")]
    public async Task<ActionResult<CourtDto>> UpdateSettingsLegacy(UpdateCourtRequest request)
    {
        var court = await _court.UpdateCourtSettingsAsync(request);
        return Ok(court);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("legacy/blocked-dates")]
    public async Task<ActionResult<List<BlockedDateDto>>> GetBlockedDatesLegacy()
    {
        var clientId = await GetClientId();
        return Ok(await _court.GetBlockedDatesAsync(null, clientId));
    }

    [Authorize(Roles = "admin")]
    [HttpPost("legacy/blocked-dates")]
    public async Task<ActionResult<BlockedDateDto>> AddBlockedDateLegacy(CreateBlockedDateRequest request)
    {
        var clientId = await GetClientId();
        var result = await _court.AddBlockedDateAsync(request, null, clientId);
        return CreatedAtAction(nameof(GetBlockedDatesLegacy), result);
    }

    // ========================================
    // ✅ PRICE RULES ENDPOINTS
    // ========================================

    [HttpGet("price-rules")]
    public async Task<ActionResult<List<PriceRuleDto>>> GetPriceRules()
    {
        var clientId = await GetClientId();
        var rules = await _court.GetPriceRulesAsync(clientId);
        return Ok(rules);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("price-rules")]
    public async Task<ActionResult<PriceRuleDto>> CreatePriceRule(CreatePriceRuleRequest request)
    {
        var clientId = await GetClientId();
        var rule = await _court.CreatePriceRuleAsync(request, clientId);
        return CreatedAtAction(nameof(GetPriceRules), rule);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("price-rules/{id}")]
    public async Task<ActionResult<PriceRuleDto>> UpdatePriceRule(Guid id, UpdatePriceRuleRequest request)
    {
        var clientId = await GetClientId();
        var rule = await _court.UpdatePriceRuleAsync(id, request, clientId);
        return Ok(rule);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("price-rules/{id}")]
    public async Task<IActionResult> DeletePriceRule(Guid id)
    {
        var clientId = await GetClientId();
        await _court.DeletePriceRuleAsync(id, clientId);
        return NoContent();
    }
}