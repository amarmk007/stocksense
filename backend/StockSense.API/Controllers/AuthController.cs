using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockSense.API.Data;
using StockSense.API.Models;

namespace StockSense.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var frontendUrl = config["CorsOrigin"] ?? "http://localhost:5173";

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Redirect($"{frontendUrl}/auth?error=auth_failed");

        var email = result.Principal!.FindFirst(ClaimTypes.Email)?.Value;
        var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            return Redirect($"{frontendUrl}/auth?error=missing_claims");

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user == null)
        {
            user = new User { Email = email, GoogleId = googleId };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var token = GenerateJwt(user);
        return Redirect($"{frontendUrl}#token={token}");
    }

    private string GenerateJwt(User user)
    {
        var secret = config["JwtSecret"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "dev-secret-change-in-production-min-32-chars!!";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsOnboarded", user.IsOnboarded.ToString().ToLower()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
