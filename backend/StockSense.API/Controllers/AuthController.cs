using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    [HttpGet("google")]
    public IActionResult GoogleLogin() => Ok("placeholder");

    [HttpGet("google/callback")]
    public IActionResult GoogleCallback() => Ok("placeholder");
}
