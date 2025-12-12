using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIMS.Models;

public class ClassSession
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Range(0,6)]
    public int DayOfWeek { get; set; } // 0=Sunday ... 6=Saturday

    [Range(1, 6)]
    public int SessionSlot { get; set; } = 1; // Ca 1..6

    [DataType(DataType.Date)]
    public DateOnly StartTime { get; set; }
    [DataType(DataType.Date)]
    public DateOnly EndTime { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }
}
