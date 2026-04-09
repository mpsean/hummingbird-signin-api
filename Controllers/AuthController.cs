using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoginApi.Data;
using LoginApi.DTOs;
using LoginApi.Models;
using LoginApi.Services;

namespace LoginApi.Controllers;

[ApiController]
[Produces("application/json")]
public class AuthController(IAuthService authService, IJwtService jwtService, AppDbContext db) : ControllerBase
{
    // ── Mappers ───────────────────────────────────────────────────────────────

    private static TenantDto MapTenant(Tenant t) => new(
        t.Id, t.Slug, t.Name, t.FrontendUrl, t.IsActive, t.CreatedAt);

    private static UserDto MapUser(User u) => new(
        u.Id, u.Email, u.Username, u.FirstName, u.LastName,
        u.Role, MapTenant(u.Tenant), u.CreatedAt, u.LastLoginAt);

    // ── Tenant Endpoints ──────────────────────────────────────────────────────

    /// <summary>Create a new tenant (admin only)</summary>
    [HttpPost("api/tenants")]
    [ProducesResponseType(typeof(TenantDto), 201)]
    [ProducesResponseType(typeof(MessageResponse), 400)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var slugExists = await db.Tenants.AnyAsync(t => t.Slug == request.Slug.ToLower());
        if (slugExists)
            return BadRequest(new MessageResponse($"Tenant slug '{request.Slug}' is already taken."));

        var tenant = new Tenant
        {
            Slug        = request.Slug.ToLower().Trim(),
            Name        = request.Name.Trim(),
            FrontendUrl = request.FrontendUrl.Trim()
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        return Created($"/api/tenants/{tenant.Id}", MapTenant(tenant));
    }

    /// <summary>List all tenants</summary>
    [HttpGet("api/tenants")]
    [ProducesResponseType(typeof(List<TenantDto>), 200)]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await db.Tenants
            .OrderBy(t => t.Name)
            .Select(t => MapTenant(t))
            .ToListAsync();

        return Ok(tenants);
    }

    /// <summary>Get a single tenant by slug</summary>
    [HttpGet("api/tenants/{slug}")]
    [ProducesResponseType(typeof(TenantDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTenant(string slug)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLower());
        if (tenant is null) return NotFound(new MessageResponse("Tenant not found."));
        return Ok(MapTenant(tenant));
    }

    // ── Auth Endpoints ────────────────────────────────────────────────────────

    /// <summary>Register a new user under a tenant</summary>
    [HttpPost("api/auth/register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(typeof(MessageResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error, user) = await authService.RegisterAsync(request);
        if (!success) return BadRequest(new MessageResponse(error!));

        var token = jwtService.GenerateToken(user!);
        return Created("", new AuthResponse(
            token, "Bearer", jwtService.ExpiryMinutes * 60,
            user!.Tenant.FrontendUrl,
            MapUser(user)));
    }

    /// <summary>Login — returns JWT and the tenant frontend URL to redirect to</summary>
    [HttpPost("api/auth/login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(MessageResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, user) = await authService.LoginAsync(request);
        if (!success) return Unauthorized(new MessageResponse(error!));

        var token = jwtService.GenerateToken(user!);
        return Ok(new AuthResponse(
            token, "Bearer", jwtService.ExpiryMinutes * 60,
            user!.Tenant.FrontendUrl,
            MapUser(user)));
    }

    /// <summary>Get current user profile</summary>
    [HttpGet("api/auth/me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub") ?? "0");

        var user = await authService.GetUserByIdAsync(userId);
        if (user is null) return NotFound(new MessageResponse("User not found."));

        return Ok(MapUser(user));
    }

    /// <summary>Change password</summary>
    [HttpPost("api/auth/change-password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(MessageResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub") ?? "0");

        var (success, error) = await authService.ChangePasswordAsync(userId, request);
        if (!success) return BadRequest(new MessageResponse(error!));

        return Ok(new MessageResponse("Password changed successfully."));
    }
}
