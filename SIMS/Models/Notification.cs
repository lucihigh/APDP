using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Message { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    [ForeignKey(nameof(UserId))]
    public IdentityUser? User { get; set; }
}

