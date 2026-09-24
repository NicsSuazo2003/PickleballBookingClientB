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
    private readonly IClientService _clientService;

    public OpenPlayController(
        IOpenPlayService openPlay,
        ClientResolver clientResolver,
        IClientService clientService)
    {
        _openPlay = openPlay;
        _clientResolver = clientResolver;
        _clientService = clientService;
    }

    private async Task<Guid> GetClientId()
    {
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            throw new UnauthorizedAccessException("Client identification required");

        try
        {
            return await _clientService.GetClientIdBySubdomainAsync(subdomain);
        }
        catch (KeyNotFoundException)
        {
            throw new UnauthorizedAccessException($"Client not found for subdomain: {subdomain}");
        }
    }

    [HttpGet("open-play")]
    public async Task<ActionResult<List<OpenPlaySessionDto>>> GetAll()
    {
        var clientId = await GetClientId();
        var sessions = await _openPlay.GetUpcomingSessionsAsync(clientId);
        return Ok(sessions);
    }

    [HttpGet("open-play/{id}")]
    public async Task<ActionResult<OpenPlaySessionDto>> GetById(Guid id)
    {
        var clientId = await GetClientId();
        var session = await _openPlay.GetSessionByIdAsync(id, clientId);
        if (session == null) return NotFound(new { message = "Open Play session not found" });
        return Ok(session);
    }
    /// <summary>
    /// Public roster for an Open Play session. Returns first-name + last-initial only.
    /// No authentication required — this is what the public session details modal uses.
    /// </summary>
    [HttpGet("open-play/{id}/players/public")]
    public async Task<ActionResult<List<PublicOpenPlayPlayerDto>>> GetPublicPlayers(Guid id)
    {
        var clientId = await GetClientId();

        try
        {
            var players = await _openPlay.GetPublicPlayersAsync(id, clientId);
            return Ok(players);
        }
        catch (KeyNotFoundException)
        {
            // Return 404 for both "session not found" and "session ended" so we don't
            // leak whether a session ever existed to anonymous callers.
            return NotFound(new { message = "Open Play session not found" });
        }
    }

    [HttpPost("open-play/{id}/join")]
    public async Task<ActionResult<BookingDto>> Join(Guid id, JoinOpenPlayRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _openPlay.JoinSessionAsync(id, request, clientId);
        return Ok(booking);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin/open-play")]
    public async Task<ActionResult<List<OpenPlaySessionDto>>> AdminGetAll()
    {
        var clientId = await GetClientId();
        var sessions = await _openPlay.AdminGetAllSessionsAsync(clientId);
        return Ok(sessions);
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
        var session = await _openPlay.AdminUpdateSessionAsync(id, request, clientId);
        return Ok(session);
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
        var players = await _openPlay.AdminGetPlayersAsync(id, clientId);
        return Ok(players);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin/open-play/{id}/stats")]
    public async Task<ActionResult<OpenPlaySessionStatsDto>> AdminGetStats(Guid id)
    {
        var clientId = await GetClientId();
        var stats = await _openPlay.AdminGetSessionStatsAsync(id, clientId);
        return Ok(stats);
    }
}