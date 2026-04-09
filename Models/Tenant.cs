using System.ComponentModel.DataAnnotations;

namespace LoginApi.Models;

public class Tenant
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Slug { get; set; } = string.Empty;   // e.g. "hotel-grand", used in routing

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;   // e.g. "Hotel Grand Bangkok"

    [Required, MaxLength(500)]
    public string FrontendUrl { get; set; } = string.Empty; // e.g. "https://grand.yourdomain.com"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<User> Users { get; set; } = [];
}
