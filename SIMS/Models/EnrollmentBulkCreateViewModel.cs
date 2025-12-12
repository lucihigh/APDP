using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SIMS.Models;

public class EnrollmentBulkCreateViewModel
{
    [Required]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Select at least one student")]
    [Display(Name = "Students")]
    public List<int> StudentIds { get; set; } = new();

    [StringLength(20)]
    public string? Semester { get; set; }

    [StringLength(5)]
    [RegularExpression(@"^(10(\.0+)?|[1-9](\.[0-9]+)?)$|^$", ErrorMessage = "Grade must be a number between 1 and 10.")]
    public string? Grade { get; set; }

    public IEnumerable<SelectListItem> Courses { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Students { get; set; } = Array.Empty<SelectListItem>();
}
