using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/clients")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ClientResolver _clientResolver;

    public ClientController(IClientService clientService, ClientResolver clientResolver)
    {
        _clientService = clientService;
        _clientResolver = clientResolver;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicSettings()
    {
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            return NotFound();

        var client = await _clientService.GetClientBySubdomainAsync(subdomain);
        return Ok(client);
    }
}