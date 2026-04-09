using System.ComponentModel.DataAnnotations;

namespace LoginApi.DTOs;

// ── Tenant ────────────────────────────────────────────────────────────────────

public record CreateTenantRequest(
    [Required, MinLength(2), MaxLength(50)]  string Slug,
    [Required, MinLength(2), MaxLength(150)] string Name,
    [Required, Url]                          string FrontendUrl
);

public record TenantDto(
    int    Id,
    string Slug,
    string Name,
    string FrontendUrl,
    bool   IsActive,
    DateTime CreatedAt
);

// ── Auth ──────────────────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required]                               string TenantSlug,    // which tenant to register under
    [Required, EmailAddress]                 string Email,
    [Required, MinLength(3), MaxLength(50)]  string Username,
    [Required, MinLength(8)]                 string Password,
    string? FirstName,
    string? LastName
);

public record LoginRequest(
    [Required] string TenantSlug,           // which tenant to login to
    [Required] string EmailOrUsername,
    [Required] string Password
);

public record AuthResponse(
    string    Token,
    string    TokenType,
    int       ExpiresIn,
    string    RedirectUrl,                  // tenant frontend URL to redirect after login
    UserDto   User
);

public record UserDto(
    int      Id,
    string   Email,
    string   Username,
    string?  FirstName,
    string?  LastName,
    string   Role,
    TenantDto Tenant,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword
);

public record MessageResponse(string Message);
