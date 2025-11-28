using System.ComponentModel.DataAnnotations;

namespace SIMS.Models;

public class Course
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 10)]
    public int Credits { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

