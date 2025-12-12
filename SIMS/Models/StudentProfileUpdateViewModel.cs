using System.ComponentModel.DataAnnotations;

namespace SIMS.Models;

public class StudentProfileUpdateViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    // Stored as text to avoid culture-related parsing issues on DateOnly binding
    [Display(Name = "DateOfBirth")]
    public string? DateOfBirth { get; set; }

    [StringLength(25)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public string? Program { get; set; }
    public int? Year { get; set; }
    public string? Email { get; set; }
}
