using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIMS.Models;

public class Assessment
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }

    [StringLength(500)]
    public string? AttachmentPath { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }
}

