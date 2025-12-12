using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Models;

public class Student
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

    [StringLength(25)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Program { get; set; }

    [Range(1, 10)]
    public int? Year { get; set; }

    [Range(0, 4)]
    public double? GPA { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public IdentityUser? User { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [NotMapped]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
