using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("placeholder");

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new { status = "pending" });
}
