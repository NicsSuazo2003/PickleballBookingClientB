// Controllers/OpenPlayController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api")]
public class OpenPlayController : ControllerBase
{
    private readonly IOpenPlayService _openPlay;
    private readonly ClientResolver _clientResolver;

    public OpenPlayController(IOpenPlayService openPlay, ClientResolver clientResolver)
    {
        _openPlay = openPlay;
        _clientResolver = clientResolver;
    }

    // ✅ Fix: 409 (not 401) on missing tenant
    private Task<Guid> GetClientId() => _clientResolver.ResolveClientIdAsync();

    [HttpGet("open-play")]
    public async Task<ActionResult<List<OpenPlaySessionDto>>> GetAll()
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.GetUpcomingSessionsAsync(clientId));
    }

    [HttpGet("open-play/{id}")]
    public async Task<ActionResult<OpenPlaySessionDto>> GetById(Guid id)
    {
        var clientId = await GetClientId();
        var session = await _openPlay.GetSessionByIdAsync(id, clientId);
        if (session == null) return NotFound(new { message = "Open Play session not found" });
        return Ok(session);
    }

    [HttpGet("open-play/{id}/players/public")]
    public async Task<ActionResult<List<PublicOpenPlayPlayerDto>>> GetPublicPlayers(Guid id)
    {
        var clientId = await GetClientId();

        try
        {
            return Ok(await _openPlay.GetPublicPlayersAsync(id, clientId));
        }
        catch (KeyNotFoundException)
        {
            // 404 for both "not found" and "ended" — no existence leak
            return NotFound(new { message = "Open Play session not found" });
        }
    }

    [HttpPost("open-play/{id}/join")]
    public async Task<ActionResult<BookingDto>> Join(Guid id, JoinOpenPlayRequest request)
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.JoinSessionAsync(id, request, clientId));
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin/open-play")]
    public async Task<ActionResult<List<OpenPlaySessionDto>>> AdminGetAll()
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.AdminGetAllSessionsAsync(clientId));
    }

    [Authorize(Roles = "admin")]
    [HttpPost("admin/open-play")]
    public async Task<ActionResult<OpenPlaySessionDto>> AdminCreate(CreateOpenPlaySessionRequest request)
    {
        var clientId = await GetClientId();
        var session = await _openPlay.AdminCreateSessionAsync(request, clientId);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("admin/open-play/{id}")]
    public async Task<ActionResult<OpenPlaySessionDto>> AdminUpdate(Guid id, UpdateOpenPlaySessionRequest request)
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.AdminUpdateSessionAsync(id, request, clientId));
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("admin/open-play/{id}")]
    public async Task<IActionResult> AdminDelete(Guid id)
    {
        var clientId = await GetClientId();
        await _openPlay.AdminDeleteSessionAsync(id, clientId);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin/open-play/{id}/players")]
    public async Task<ActionResult<List<OpenPlayPlayerDto>>> AdminGetPlayers(Guid id)
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.AdminGetPlayersAsync(id, clientId));
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin/open-play/{id}/stats")]
    public async Task<ActionResult<OpenPlaySessionStatsDto>> AdminGetStats(Guid id)
    {
        var clientId = await GetClientId();
        return Ok(await _openPlay.AdminGetSessionStatsAsync(id, clientId));
    }
}