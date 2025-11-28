using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIMS.Models;

public class Enrollment
{
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [StringLength(20)]
    public string? Semester { get; set; }

    [StringLength(5)]
    public string? Grade { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }
}

