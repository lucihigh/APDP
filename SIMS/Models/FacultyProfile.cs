using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Models;

public class FacultyProfile
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public IdentityUser? User { get; set; }

    [NotMapped]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
