using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("placeholder");

    [HttpPost]
    public IActionResult Create() => Ok("placeholder");

    [HttpPatch]
    public IActionResult Update() => Ok("placeholder");
}
