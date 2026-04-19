using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
public class AuthController(AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? config["GoogleClientId"] ?? "";
        var redirectUri = GetCallbackUri();
        var state = GenerateStateJwt();
        var scope = Uri.EscapeDataString("openid email profile");

        var googleUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={scope}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&access_type=online";

        return Redirect(googleUrl);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(string? code, string? state, string? error)
    {
        var frontendUrl = Environment.GetEnvironmentVariable("CORS_ORIGIN")
            ?? config["CorsOrigin"]
            ?? "http://localhost:5173";

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect($"{frontendUrl}/auth?error=auth_failed");

        if (!ValidateStateJwt(state))
            return Redirect($"{frontendUrl}/auth?error=invalid_state");

        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? config["GoogleClientId"] ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? config["GoogleClientSecret"] ?? "";
        var redirectUri = GetCallbackUri();

        var http = httpClientFactory.CreateClient();

        var tokenResp = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
            }));

        if (!tokenResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth?error=token_exchange_failed");

        var tokenJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResp = await http.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

        if (!userInfoResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth?error=userinfo_failed");

        var userInfo = await userInfoResp.Content.ReadFromJsonAsync<JsonElement>();
        var email = userInfo.GetProperty("email").GetString();
        var googleId = userInfo.GetProperty("sub").GetString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            return Redirect($"{frontendUrl}/auth?error=missing_claims");

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user == null)
        {
            user = new User { Email = email, GoogleId = googleId };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        return Redirect($"{frontendUrl}?token={GenerateJwt(user)}");
    }

    private string GetCallbackUri()
    {
        var host = Request.Host.Value;
        return $"https://{host}/api/auth/google/callback";
    }

    private string JwtSecret() =>
        config["JwtSecret"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "dev-secret-change-in-production-min-32-chars!!";

    private string GenerateStateJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret()));
        var token = new JwtSecurityToken(
            claims: [new Claim("nonce", Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool ValidateStateJwt(string state)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret()));
            new JwtSecurityTokenHandler().ValidateToken(state, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch { return false; }
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret()));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsOnboarded", user.IsOnboarded.ToString().ToLower()),
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
