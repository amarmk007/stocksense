using StockSense.API.Data;
using StockSense.API.Jobs;
using StockSense.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Listen on Railway's injected PORT (or fallback for local dev)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Database — Railway provides postgresql:// URL; convert to Npgsql format if needed
var rawDb = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "";
var connectionString = ConvertDatabaseUrl(rawDb);

static string ConvertDatabaseUrl(string url)
{
    if (!url.StartsWith("postgresql://") && !url.StartsWith("postgres://"))
        return url;
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// CORS
var corsOrigin = Environment.GetEnvironmentVariable("CORS_ORIGIN")
    ?? builder.Configuration["CorsOrigin"]
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// JWT Auth
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSecret"]
    ?? "dev-secret-change-in-production-min-32-chars";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? builder.Configuration["GoogleClientId"]
        ?? "placeholder";
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? builder.Configuration["GoogleClientSecret"]
        ?? "placeholder";
    options.CallbackPath = "/api/auth/google/callback";
});

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

// External services
builder.Services.AddHttpClient("finnhub", c =>
    c.BaseAddress = new Uri("https://finnhub.io"));
builder.Services.AddHttpClient("edgar", c =>
{
    c.BaseAddress = new Uri("https://efts.sec.gov");
    c.DefaultRequestHeaders.Add("User-Agent", "StockSense/1.0 contact@stocksense.dev");
});
builder.Services.AddScoped<FinnhubService>();
builder.Services.AddScoped<EdgarService>();
builder.Services.AddScoped<ClaudeService>();
builder.Services.AddScoped<DailyRecommendationJob>();

// Controllers (global [Authorize] applied per-controller; auth endpoints use [AllowAnonymous])
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard — localhost only in dev, disabled in prod
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
    });
}

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapControllers();

// Schedule daily recommendation job
var cron = builder.Configuration["HangfireDailyCron"] ?? "0 5 * * *";
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<DailyRecommendationJob>("daily-recommendations", j => j.ExecuteAsync(), cron);

app.Run();
