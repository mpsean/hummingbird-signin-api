using Microsoft.EntityFrameworkCore;
using LoginApi.Data;
using LoginApi.DTOs;
using LoginApi.Models;

namespace LoginApi.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, User? User)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, User? User)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error)>             ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<User?> GetUserByIdAsync(int id);
}

public class AuthService(AppDbContext db, ILogger<AuthService> logger) : IAuthService
{
    public async Task<(bool Success, string? Error, User? User)> RegisterAsync(RegisterRequest request)
    {
        // Resolve tenant
        var tenant = await db.Tenants.FirstOrDefaultAsync(t =>
            t.Slug == request.TenantSlug.ToLower() && t.IsActive);

        if (tenant is null)
            return (false, $"Tenant '{request.TenantSlug}' not found or inactive.", null);

        // Check uniqueness within tenant
        var emailExists = await db.Users.AnyAsync(u =>
            u.TenantId == tenant.Id && u.Email == request.Email.ToLower());
        if (emailExists) return (false, "Email is already registered.", null);

        var usernameExists = await db.Users.AnyAsync(u =>
            u.TenantId == tenant.Id && u.Username == request.Username.ToLower());
        if (usernameExists) return (false, "Username is already taken.", null);

        var user = new User
        {
            TenantId     = tenant.Id,
            Email        = request.Email.ToLower().Trim(),
            Username     = request.Username.ToLower().Trim(),
            PasswordHash = request.Password,
            FirstName    = request.FirstName?.Trim(),
            LastName     = request.LastName?.Trim()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Reload with tenant
        await db.Entry(user).Reference(u => u.Tenant).LoadAsync();

        logger.LogInformation("New user registered: {Email} under tenant: {Tenant}",
            user.Email, tenant.Slug);

        return (true, null, user);
    }

    public async Task<(bool Success, string? Error, User? User)> LoginAsync(LoginRequest request)
    {
        // Resolve tenant
        var tenant = await db.Tenants.FirstOrDefaultAsync(t =>
            t.Slug == request.TenantSlug.ToLower() && t.IsActive);

        if (tenant is null)
            return (false, "Invalid credentials.", null);

        var identifier = request.EmailOrUsername.ToLower().Trim();

        var user = await db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u =>
                u.TenantId == tenant.Id &&
                (u.Email == identifier || u.Username == identifier));

        if (user is null || !user.IsActive)
            return (false, "Invalid credentials.", null);

        if (request.Password != user.PasswordHash)
            return (false, "Invalid credentials.", null);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("User logged in: {Email} tenant: {Tenant}",
            user.Email, tenant.Slug);

        return (true, null, user);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found.");

        if (request.CurrentPassword != user.PasswordHash)
            return (false, "Current password is incorrect.");

        user.PasswordHash = request.NewPassword;
        await db.SaveChangesAsync();

        return (true, null);
    }

    public Task<User?> GetUserByIdAsync(int id) =>
        db.Users
          .Include(u => u.Tenant)
          .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
}
